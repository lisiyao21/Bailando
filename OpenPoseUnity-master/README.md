# OpenPose-Rig
tf-openpose and unity IK

this Unity project read tf-openpose data and move avators with IK or humanoid bones

YouTube

[![](https://img.youtube.com/vi/VJkKxBRpmtE/0.jpg)](https://www.youtube.com/watch?v=VJkKxBRpmtE)

# How to Use
Just play Unity editor.
Now, Unity-chan moves according to posture information data.
Attitude information data follows the naming convention.

>./Assets/(Data_Path)/(FileName)+(numbers starting from 0).txt

Posture information is the one frame pose estimation result output as txt. 
For example, ./Assets/data_Doit/1000.txt is shown below.

>[[[  40.14869238   25.0827944    34.79516988  175.62203954   54.81346927
   -100.59037752   57.12147974   47.59692211    7.5893725  -111.67703685
    -97.95432334   19.7687591   101.26821028  -28.22508002  -16.54799451
    -68.87994546 -187.91484028]
  [  12.13893678  165.73579598  152.11861955  134.05310831 -141.45767522
   -152.57585959 -125.2229218     8.80272922  -19.46985321  -16.11921428
    -20.46784218 -170.66696495 -290.39149823 -285.19754306  145.97982669
    329.49580272  279.11868701]
  [ -96.93716987 -177.03857125 -588.93828193 -959.98628182 -181.36536777
   -617.07236162 -975.96549378  191.5918761   449.33956014  567.43281962
    643.62707019  419.39150013   92.59446927 -190.12443007  430.91581316
    272.80288035  283.6713988 ]]]

This is posture data st 1000 frame in this video.

[Shia LaBeouf "Just Do It" Motivational Speech (Original Video by LaBeouf, Rönkkö & Turner)](https://www.youtube.com/watch?v=ZXsQAXx_ao0)

# Move by IK
Required components

![RequireComponents4IK](https://github.com/keel-210/OpenPose-Rig/blob/Images/IK_full.png)

![IKMover](https://github.com/keel-210/OpenPose-Rig/blob/Images/IK.png)

This component use this IK.
[SAFullBodyIK](https://github.com/Stereoarts/SAFullBodyIK)

Data must be named ./Assets/(Data_Path)/(FileName)+(numbers starting from 0).txt
![DataStructure](https://github.com/keel-210/OpenPose-Rig/blob/Images/data_structure.png)


# Move by bone-rotation
Required components

![RequireComponents4Rotater](https://github.com/keel-210/OpenPose-Rig/blob/Images/boneRot_full.png)

![BoneRotater](https://github.com/keel-210/OpenPose-Rig/blob/Images/bone_rotate.png)

Data must be named ./Assets/(Data_Path)/(FileName)+(numbers starting from 0).txt
![DataStructure](https://github.com/keel-210/OpenPose-Rig/blob/Images/data_structure.png)



More Info : https://qiita.com/keel/items/0d64167850566586d22a
