import pickle
import numpy as np
import os

filePath = './keypoints3d/'
savePath = './data_'
dataList = []
with open(filePath+'gBR_sBM_cAll_d04_mBR3_ch02.pkl','rb') as f:
    while True:
        try:
            data = pickle.load(f)
        except EOFError:
                break
        
        dataList.append(data)

k = 0
tmpPath = savePath+str(k)
if not os.path.exists(tmpPath):
    os.makedirs(tmpPath)

for i in range(dataList[k]['keypoints3d_optim'].shape[0]):
    tmpData = np.transpose(dataList[k]['keypoints3d_optim'][i])
    tmpData = tmpData.tolist()
    tmpData = [tmpData]

    tmpPath = savePath+str(k)
    if not os.path.exists(tmpPath):
        os.makedirs(tmpPath)
    
    with open(tmpPath+'/'+str(i)+'.txt','w') as f:
        f.writelines("%s" % tmpData)
