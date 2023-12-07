#!/bin/sh
currenttime=`date "+%Y%m%d%H%M%S"`
if [ ! -d log ]; then
    mkdir log
fi

echo "[Usage] ./srun.sh config_path [train|eval] partition gpunum"
# check config exists
if [ ! -e $1 ]
then
    echo "[ERROR] configuration file: $1 does not exists!"
    exit
fi


if [ ! -d ${expname} ]; then
    mkdir ${expname}
fi

echo "[INFO] saving results to, or loading files from: "$expname

if [ "$3" == "" ]; then
    echo "[ERROR] enter partition name"
    exit
fi
partition_name=$3
echo "[INFO] partition name: $partition_name"

if [ "$4" == "" ]; then
    echo "[ERROR] enter gpu num"
    exit
fi
gpunum=$4
gpunum=$(($gpunum<8?$gpunum:8))
echo "[INFO] GPU num: $gpunum"
((ntask=$gpunum*3))


TOOLS="srun --mpi=pmi2 --partition=$partition_name --cpus-per-task=8 --gres=gpu:$gpunum -n1 -w SG-IDC2-10-51-5-45 --job-name=${config_suffix}"
PYTHONCMD="python -u main_mix.py --config $1"

if [ $2 == "train" ];
then
    $TOOLS $PYTHONCMD \
    --train 
elif [ $2 == "eval" ];
then
    $TOOLS $PYTHONCMD \
    --eval 
elif [ $2 == "visgt" ];
then
    $TOOLS $PYTHONCMD \
    --visgt 
elif [ $2 == "anl" ];
then
    $TOOLS $PYTHONCMD \
    --anl 
elif [ $2 == "sample" ];
then
    $TOOLS $PYTHONCMD \
    --sample 
fi

