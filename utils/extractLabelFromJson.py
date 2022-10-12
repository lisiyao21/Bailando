import json
import numpy as np
import os
from tqdm import tqdm
import csv

FilePath='./labelData/'

def getAllFileNamesOnThisDirectory(pathroot, isAddDir=False):
    fileNames = []
    for fileName in os.listdir(pathroot):
        if os.path.isdir(os.path.join(pathroot, fileName)):
            if isAddDir:
                pass
            else:
                continue
        fileNames.append(fileName)
    return fileNames

def loadAllJsonOnThisDirectory(pathroot):
    datas = []
    fileNames = getAllFileNamesOnThisDirectory(pathroot)
    for fileName in tqdm(fileNames):
        currentFilePath = os.path.join(pathroot, fileName)
        tmpjson = None
        with open(currentFilePath,'rb') as f:
            tmpjson = json.load(f)
        
        datas.append(tmpjson)
    return datas


dddd = loadAllJsonOnThisDirectory(FilePath)
fd = []
ffd = []
for ddd in tqdm(dddd):
    for dd in ddd['data']:
        verb_or_noun = 0
        if (dd['attributes'][0]['name'][-1] == '다'):
            verb_or_noun = 1
        fd.append([ddd['metaData']['name'], dd['attributes'][0]['name'],verb_or_noun])
        try:
            if (ffd.index([dd['attributes'][0]['name'], verb_or_noun])):
                pass
        except:
            ffd.append([dd['attributes'][0]['name'],verb_or_noun])

with open('List_raw.csv','w',newline='') as f :
    write = csv.writer(f)
    write.writerows(fd)

with open('List_filtered.csv','w',newline='') as f :
    write = csv.writer(f)
    write.writerows(ffd)





