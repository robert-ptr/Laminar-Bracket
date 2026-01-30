import numpy as np
import pandas as pd

import os
import json

from scipy.stats import multivariate_normal
from wavelets.Haar import haar
from wavelets.Daubechies import db
from wavelets.Symmlet import sym


def next_2n(n):
    return int(2 ** np.ceil(np.log2(n)))

def extend_time_series(s,n,m):
    x = s.copy()
    
    gap = m-n
    
    if gap != 0:
        x = np.concatenate((x, s[(n-gap):][::-1]))
    return x

def get_matrix(s,w):
    x = []
    for i in range(len(s) - w + 1):
        x.append(s[i:i+w])
    return np.array(x)

def MLE(x):
    mean = np.mean(x, axis= 0)
    cov_s = np.cov(x, rowvar=False)
    return mean, cov_s

def log_prob_density(x, mean, cov):
    return multivariate_normal.logpdf(x, mean=mean, cov=cov, allow_singular=True)

def predict(p, z_e):
    a = np.zeros((len(p)))
    
    for i in range(len(p)):
        if p[i] < z_e:
            a[i] = 1
    
    return a

def update_anomalies(a, l, h, m):
    window = 2 ** l
    
    for i in range(len(a)):
        if a[i]:
            s = i * window
            e = min((i + 1) * window, m)
            h[s : e] += 1
    
    return h
def meanAnomaly(y, eps, B, d_max, l_start, wavelet):
    m = len(y)
    L = int(np.log2(m))
    
    if wavelet == 'haar':
        c,d = haar(y,L)
    elif wavelet == 'db4':
        c,d = db(y,L)
    else:
        c,d = sym(y,L)

    h = np.zeros(m)

    for l in range(l_start, L + 1):
        w = max(2, l - l_start + 1)
        
        D = get_matrix(d[l], w)
        if D.shape[0] > 1:
            
            mu, Sigma = MLE(D)
            p = log_prob_density(D, mu, Sigma)
            z_eps = np.quantile(p, eps)
            a = predict(p, z_eps)
            
            h = update_anomalies(a, l, h, m)

        if l < L:
            
            C = get_matrix(c[l], w)
            
            if C.shape[0] > 1:
                
                mu, Sigma = MLE(C)
                p = log_prob_density(C, mu, Sigma)
                z_eps = np.quantile(p, eps)
                a = predict(p, z_eps)

                h = update_anomalies(a, l, h, m)

    h[h < 2] = 0
    
    S = []
    i = 0
    
    while i < m:
        if h[i] > 0:
            
            anom = [i]
            B_score = h[i]
            j = i + 1
            
            while j < m and j - anom[-1] <= d_max:
                if h[j] > 0:
                    anom.append(j)
                    B_score += h[j]
                j += 1
            
            if B_score >= B:
                S.append(int(np.average(anom, weights = h[anom])))
                
            i = j
        else:
            i += 1

    return S

def vote_anomalies(anomaly_sets, vote_tol = 5, min_votes = 2):
    
    votes = {}

    for S in anomaly_sets:
        for t in S:
            
            found = False
            
            for i in votes:
                if abs(i - t) <= vote_tol:
                    votes[i] += 1
                    found = True
                    break
                
            if not found:
                votes[t] = 1

    return sorted([t for t, v in votes.items() if v >= min_votes])

def multiwavelet_anomaly_detector(y, eps, B, d_max, l_start):
    wavelets = {
        "haar": "haar",
        "db4": "db4",
        "bior": "bior3.3"
    }

    results = []
    for w in wavelets.values():
        S = meanAnomaly(
            y=y,
            eps=eps,
            B=B,
            d_max=d_max,
            l_start=l_start,
            wavelet=w
        )
        results.append(S)

    return vote_anomalies(results)


with open('data/NAB/combined_labels.json', 'r') as f:
    labels = json.load(f)

results = []
tolerance = 10

for file_path, anomaly_timestamps in labels.items():
    full_path = os.path.join('data/NAB', file_path)
    if not os.path.exists(full_path):
        continue
    
    try:
        data = pd.read_csv(full_path)
        values = data['value'].values
        
        n = len(values)
        m = next_2n(n)
        extended_values = extend_time_series(values, n, m)
        
        predicted = multiwavelet_anomaly_detector(
            extended_values,
            eps = 0.0149, B = 9, d_max = 1, l_start = 1
        )
        
        true_indices = []
        
        if anomaly_timestamps:
            data['timestamp'] = pd.to_datetime(data['timestamp'])
            for date in anomaly_timestamps:
                t = pd.to_datetime(date)

                for i in range(len(data)):
                    if data['timestamp'].iloc[i] == t:
                        true_indices.append(i)
                        break
        
        if len(predicted) == 0 and len(true_indices) == 0:
            precision = 1.0
            recall = 1.0
            f1 = 1.0
        elif len(predicted) == 0 or len(true_indices) == 0:
            if len(predicted) > 0:
                precision = 0.0
            else:
                precision = 1.0
            
            if len(true_indices) > 0:
                recall = 0.0
            else:
                recall = 1.0
                
            f1 = 0.0
        else:
            tp = 0
            for p in predicted:
                for t in true_indices:
                    if abs(p - t) <= tolerance:
                        tp += 1
                        break
            
            fp = len(predicted) - tp
            
            fn = 0
            for t in true_indices:
                found = False
                for p in predicted:
                    if abs(p - t) <= tolerance:
                        found = True
                        break
                if not found:
                    fn += 1
            
            if (tp + fp) > 0:
                precision = tp / (tp + fp)
            else:
                precision = 0.0
            
            if (tp + fn) > 0:
                recall = tp / (tp + fn)
            else:
                recall = 0.0
            
            if (precision + recall) > 0:
                f1 = 2 * precision * recall / (precision + recall)
            else:
                f1 = 0.0
        
        results.append({
            'precision': precision,
            'recall': recall,
            'f1': f1,
        })
        
        print(f"{file_path}")
        print(f"P = {precision:.4f}, R = {recall:.4f}, F1 = {f1:.4f}")
        print(f"Pred = {len(predicted)}, True = {len(true_indices)}")
        print()
    
    except Exception as e:
        print(f"{file_path} error ={str(e)}")
        continue

print()
print("Results:")
avg_precision = np.mean([r['precision'] for r in results])
avg_recall = np.mean([r['recall'] for r in results])
avg_f1 = np.mean([r['f1'] for r in results])

print(f"Average Precision: {avg_precision:.4f}")
print(f"Average Recall:    {avg_recall:.4f}")
print(f"Average F1 Score:  {avg_f1:.4f}")

