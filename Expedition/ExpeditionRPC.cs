using Expedition;

namespace RainMeadow
{
    public static class ExpeditionRPC
    {
        [RPCMethod]
        public static void challengeCreaturePinned(int index, OnlineCreature crit)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame)) return;
            if (!(ExpeditionData.challengeList[index] is PinChallenge)) throw new InvalidProgrammerException("not pin challenge");

            var cl = (PinChallenge)ExpeditionData.challengeList[index];
            if (RainMeadow.isExpeditionMode(out var ex))
            {                
                if (!ex.pinChallenge_PinList.Contains(crit))
                {
                    ex.pinChallenge_PinList.Add(crit);
                    cl.current++;
                    cl.UpdateDescription();
                }
            }
        }

        [RPCMethod]
        public static void challengeCreatureKilled(RPCEvent rpc, int index, OnlineCreature crit)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame)) return;
            var challenge = ExpeditionData.challengeList[index];
            if (challenge.completed || challenge.game == null || crit == null) return;

            CreatureTemplate.Type type = crit.abstractCreature.creatureTemplate.type;
            switch (challenge)
            {                
                case HuntChallenge huntChallenge:
                    huntChallenge.current++;
                    RainMeadow.Info("HuntChallenge - Player " + (rpc.from).ToString() + " killed " + crit);
                    huntChallenge.UpdateDescription();
                    if (huntChallenge.current >= huntChallenge.amount)
                    {
                        huntChallenge.CompleteChallenge();
                    }
                    break;

                case GlobalScoreChallenge globalScoreChallenge:
                    if (type != null && ChallengeTools.creatureSpawns[ExpeditionData.slugcatPlayer.value].Find((ChallengeTools.ExpeditionCreature f) => f.creature == type) is ChallengeTools.ExpeditionCreature globalExpeditionCrit)
                    {
                        int points = globalExpeditionCrit.points;
                        globalScoreChallenge.score += points;
                        RainMeadow.Info($"GlobalScoreChallenge - Player {rpc.from} killed {type.value} | + {points.ToString()}");
                    }
                    globalScoreChallenge.UpdateDescription();
                    if (globalScoreChallenge.score >= globalScoreChallenge.target)
                    {
                        globalScoreChallenge.score = globalScoreChallenge.target;
                        globalScoreChallenge.CompleteChallenge();
                    }
                    break;

                case CycleScoreChallenge cycleScoreChallenge:
                    if (type != null && ChallengeTools.creatureSpawns[ExpeditionData.slugcatPlayer.value].Find((ChallengeTools.ExpeditionCreature f) => f.creature == type) is ChallengeTools.ExpeditionCreature cycleExpeditionCrit)
                    {
                        int points = cycleExpeditionCrit.points;
                        cycleScoreChallenge.score += points;
                        RainMeadow.Info($"CycleScoreChallenge - Player {rpc.from } killed {type.value} | + {points.ToString()}");
                    }
                    cycleScoreChallenge.UpdateDescription();
                    if (cycleScoreChallenge.score >= cycleScoreChallenge.target)
                    {
                        cycleScoreChallenge.score = cycleScoreChallenge.target;
                        cycleScoreChallenge.CompleteChallenge();
                    }
                
                break;
            }            
        }

        [RPCMethod]
        public static void completeChallenge(int index)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame)) return;
            ExpeditionData.challengeList[index].CompleteChallenge();
            RainMeadow.Info($"{ExpeditionData.challengeList[index]} index: {index} complete");
        }

        [RPCMethod]
        public static void SlowTimePerk()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame)) return;

            for (int i = 0; i < ExpeditionGame.unlockTrackers.Count; i++)
            {
                if (ExpeditionGame.unlockTrackers[i] is ExpeditionGame.SlowTimeTracker stt)
                {
                    ((Player)stt.game.Players[0].realizedCreature).mushroomCounter = 100;
                    stt.cooldown = 10;
                }
            }
        }
    }
}