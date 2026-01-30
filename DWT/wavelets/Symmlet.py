import numpy as np

g = [-0.07576571478927333, -0.02963552764599851, 0.49761866763201545, 0.8037387518059161, 0.29785779560527736, -0.09921954357684722, -0.012603967262037833, 0.0322231006040427][::-1]
h = [-0.0322231006040427, -0.012603967262037833, 0.09921954357684722, 0.29785779560527736, -0.8037387518059161, 0.49761866763201545, 0.02963552764599851, -0.07576571478927333][::-1]

def sym(s, lvl=1):

    c = {}  
    d = {} 
    
    for k in range(1, lvl + 1):
    
        l = len(g)
        y_ext = np.pad(s, (l - 1, l - 1), mode='symmetric')

        c_aux = []
        d_aux = []
        
        for t in range((len(s) + l - 1) // 2):
            t = 2 * t + 1
            c_aux.append(round(float(np.dot(g, y_ext[t:t + l])), 8))
            d_aux.append(round(float(np.dot(h, y_ext[t:t + l])), 8))
            
        c[k] = np.array(c_aux)
        d[k] = np.array(d_aux)
        s = c[k].copy()
        
    return c, d