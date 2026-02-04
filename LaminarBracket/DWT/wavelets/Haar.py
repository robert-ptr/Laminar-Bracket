import numpy as np

rad2 = np.sqrt(2)
rad3 = np.sqrt(3)

def haar(s, lvl=1):
    n = len(s)
    
    lvl = min(int(np.log2(n)), lvl)
    
    c = {}  
    d = {}
    e = {}
    g = [rad2 / 2, rad2 / 2]
    h = [-rad2 / 2, rad2 / 2]

    for l in range(1, lvl + 1):
        n_copy = (int)(n // (2 ** l))

        y_low = []
        y_high = []

        for i in range(n_copy, 0, -1):
            low = s[2 * i - 1] * g[0] + s[2 * (i - 1)] * g[1]
            high = s[2 * i - 1] * h[0] + s[2 * (i - 1)] * h[1]

            y_low.append(round(float(low), 8))
            y_high.append(round(float(high), 8))
        
        c[l] = np.array(y_low[::-1])
        d[l] = np.array(y_high[::-1])
        s = c[l].copy()

    return c, d