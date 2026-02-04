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


datasets = [
    {
        'file': 'created_data/compression_dataset0.csv',
        'name': 'Dataset 0.5% percentile',
        'params': {'eps': 0.0100, 'B': 13, 'd_max': 3, 'l_start': 1},
        'tolerance': 10
    },
    {
        'file': 'created_data/compression_dataset1.csv',
        'name': 'Dataset 1% percentile',
        'params': {'eps': 0.0100, 'B': 5, 'd_max': 1, 'l_start': 1},
        'tolerance': 10
    }
]

for dataset_config in datasets:
    csv_file = dataset_config['file']
    dataset_name = dataset_config['name']
    params = dataset_config['params']
    tolerance = dataset_config['tolerance']
    
    print(f"\nEvaluating: {dataset_name}")
    print(f"File: {csv_file}")
    print(f"Parameters: eps={params['eps']}, B={params['B']}, d_max={params['d_max']}, l_start={params['l_start']}")
    print(f"Tolerance: {tolerance}\n")
    
    if not os.path.exists(csv_file):
        print(f"Error: {csv_file} not found")
        continue
    
    df = pd.read_csv(csv_file)
    
    sources = {}
    for source in df['SourceFile'].unique():
        source_data = df[df['SourceFile'] == source].copy()
        source_data = source_data.sort_values('ChunkIndex').reset_index(drop=True)
        sources[source] = source_data
    
    print(f"Loaded {len(sources)} source files:")
    for name, data in sources.items():
        abnormal_count = data['IsAbnormal'].sum()
        print(f"  {name}: {len(data)} chunks, {abnormal_count} abnormal")
    print()
        
    for source_name, source_data in sources.items():
        try:
            values = source_data['CompressionRatio'].values
            labels_array = source_data['IsAbnormal'].values
            
            n = len(values)
            m = next_2n(n)
            extended_values = extend_time_series(values, n, m)
            
            predicted = multiwavelet_anomaly_detector(
                extended_values,
                eps=params['eps'],
                B=params['B'],
                d_max=params['d_max'],
                l_start=params['l_start']
            )
            
            true_indices = [i for i, label in enumerate(labels_array) if label == 1]
            
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
                    if p >= len(labels_array):
                        continue
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
            
            print(f"    {source_name}")
            print(f"    P = {precision:.4f}, R = {recall:.4f}, F1 = {f1:.4f}\n")
        
        except Exception as e:
            print(f"{source_name} error = {str(e)}\n")
            continue
