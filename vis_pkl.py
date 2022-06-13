from utils.functional import visualizeAndWritefromPKL


class VSConfig():
    height = 540
    width = 960
config = VSConfig()

visualizeAndWritefromPKL('/mnt/lustre/syli/dance/Bailando/experiments/actor_critic_for_demo/vis/pkl/ep000015', config)