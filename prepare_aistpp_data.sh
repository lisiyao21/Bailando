#!/bin/sh
python _prepro_aistpp.py
python _prepro_aistpp_music.py

# for actor critic
python _prepro_aistpp_music.py --store_dir data/aistpp_music_feat --sampling_rate 15360*2

# remove bad dances; the list is from AIST++ project page
for ff in `cat ignored_list.txt`
do
    rm -rf data/aistpp_train_wav/$ff
done
