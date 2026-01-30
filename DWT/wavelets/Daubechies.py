import numpy as np

g = [0.2303778133088965, 0.7148465705529157, 0.6308807679298589, -0.027983769416859854, -0.18703481171909309, 0.030841381835560764, 0.0328830116668852, -0.010597401785069032]
h = [-0.010597401785069032, -0.0328830116668852, 0.030841381835560764, 0.18703481171909309, -0.027983769416859854, -0.6308807679298589, 0.7148465705529157, -0.2303778133088965]

def db(s, lvl = 1):
        
    c = {}  
    d = {}
    
    for k in range(1, lvl + 1):
        l = len(g)
        c_aux = []
        d_aux = []
        y_ext = np.pad(s, (l-1, l-1), mode='symmetric')

        for n in range((len(s) + l - 1) // 2):
            i = 2*n + 1
            c_aux.append(round(float(np.dot(g, y_ext[i : i + l])), 8))
            d_aux.append(round(float(np.dot(h, y_ext[i : i + l])), 8))
        
        c[k] = np.array(c_aux)
        d[k] = np.array(d_aux)
        s = c[k].copy()
    
    return c, d