﻿using Pk3DSRNGTool.RNG;
using System.Linq;
using System.Collections.Generic;

namespace Pk3DSRNGTool.Core
{
    internal static class RNGPool
    {
        public static int index { get; private set; }
        public static void ResetIndex() => index = 0;
        public static void Advance(int d) => index += d;

        private static List<uint> RandList = new List<uint>();
        private static List<ulong> RandList64 = new List<ulong>();
        public static uint getrand => RandList[index++];
        public static ulong getrand64 => RandList64[index++];

        private static List<string> RNGStateStr = new List<string>();

        public static bool Considerdelay;
        public static int DelayTime;

        public static void Clear()
        {
            RandList.Clear();
            RandList64.Clear();
            RNGStateStr.Clear();
        }

        public static void CreateBuffer(int buffersize, IRNG rng)
        {
            if (rng is IRNG64 rng64)
            {
                for (int i = 0; i < buffersize; i++)
                    RandList64.Add(rng64.Nextulong());
                return;
            }
            for (int i = 0; i < buffersize; i++)
            {
                RNGStateStr.Add((rng as RNGState)?.CurrentState());
                RandList.Add(rng.Nextuint());
            }
        }

        public static void Next(IRNG rng)
        {
            if (rng is IRNG64 rng64)
            {
                RandList64.RemoveAt(0);
                RandList64.Add(rng64.Nextulong());
                return;
            }
            RNGStateStr.RemoveAt(0);
            RNGStateStr.Add((rng as RNGState)?.CurrentState());
            RandList.RemoveAt(0);
            RandList.Add(rng.Nextuint());
        }

        public static Pokemon PM;
        public static bool IsMainRNGEgg;
        public static IGenerator igenerator;

        public static RNGResult Generate6()
        {
            index = Considerdelay ? DelayTime : 0;
            Advance(1);
            var result = getresult6() as Result6;
            result.RandNum = RandList[0];
            result.Status = RNGStateStr[0];
            return result;
        }

        public static RNGResult GenerateEgg6()
        {
            index = Considerdelay ? DelayTime : 0;
            Advance(1);
            var result = GenerateAnEgg6(new uint[] { getrand, getrand });
            result.RandNum = RandList[0];
            result.Status = RNGStateStr[0];
            return result;
        }

        public static EggResult GenerateAnEgg6(uint[] key)
        {
            Egg6.ReSeed(key);
            var result = (igenerator as Egg6).Generate() as EggResult;
            result.EggSeed = key[0] | ((ulong)key[1] << 32);
            return result;
        }

        public static RNGResult getresult6()
        {
            switch (igenerator)
            {
                case Stationary6 sta_rng:
                    return sta_rng.Generate();
                case Event6 event_rng:
                    return event_rng.Generate();
                case Wild6 wild_rng:
                    return wild_rng.Generate();
            }
            return null;
        }

        public static RNGResult Generate7()
        {
            index = 0;
            int frameshift = getframeshift();
            var result = getresult7() as Result7;
            result.RandNum = RandList64[0];
            result.FrameDelayUsed = frameshift;
            return result;
        }

        public static RNGResult GenerateEgg7()
        {
            index = 0;
            var result = (igenerator as Egg7).Generate() as EggResult;
            result.RandNum = RandList[0];
            result.Status = RNGStateStr[0];
            result.FramesUsed = index;
            return result;
        }

        public static RNGResult getresult7()
        {
            switch (igenerator)
            {
                case Stationary7 sta_rng:
                    return IsMainRNGEgg ? sta_rng.GenerateMainRNGPID(firstegg) : sta_rng.Generate();
                case Event7 event_rng:
                    return event_rng.Generate();
                case Wild7 wild_rng:
                    return wild_rng.Generate();
            }
            return null;
        }

        #region Gen7 Time keeping

        public static bool IsSolgaleo, IsLunala, IsExeggutor;
        public static byte modelnumber;
        public static int[] remain_frame;

        public static bool route17, phase;
        public static int PreHoneyCorrection;

        public static void ResetModelStatus()
        {
            remain_frame = new int[modelnumber];
            phase = false;
        }

        public static void CopyStatus(ModelStatus st)
        {
            modelnumber = st.Modelnumber;
            remain_frame = (int[])st.remain_frame.Clone();
            phase = st.phase;
        }

        public static void time_elapse(int n)
        {
            for (int totalframe = 0; totalframe < n; totalframe++)
            {
                for (int i = 0; i < modelnumber; i++)
                {
                    if (remain_frame[i] > 1)                       //Cooldown 2nd part
                    {
                        remain_frame[i]--;
                        continue;
                    }
                    if (remain_frame[i] < 0)                       //Cooldown 1st part
                    {
                        if (++remain_frame[i] == 0)                //Blinking
                            remain_frame[i] = (int)(getrand64 % 3) == 0 ? 36 : 30;
                        continue;
                    }
                    if ((int)(getrand64 & 0x7F) == 0)              //Not Blinking
                        remain_frame[i] = -5;
                }
                if (route17 && (phase = !phase))
                    Advance(2);
            }
        }

        //model # changes when screen turns black
        private static void SolLunaRearrange()
        {
            modelnumber = 5;//2 guys offline...
            int[] order = { 0, 1, 2, 5, 6 };
            for (int i = 0; i < 5; i++)
                remain_frame[i] = remain_frame[order[i]];
        }

        //Another type of change (Lillie)
        private static void ExeggutorRearrange()
        {
            modelnumber = 2;
            int tmp = remain_frame[0];
            remain_frame = new int[2];
            remain_frame[0] = tmp;
        }

        private static void time_delay()
        {
            time_elapse(2); // Buttom press delay
            if (IsSolgaleo || IsLunala)
            {
                int crydelay = IsSolgaleo ? 79 : 76;
                time_elapse(DelayTime - crydelay - 19);
                if (modelnumber == 7) SolLunaRearrange();
                time_elapse(19);
                Advance(1);     //Cry Inside Time Delay
                time_elapse(crydelay);
                return;
            }
            if (IsExeggutor)
            {
                time_elapse(1);
                if (modelnumber == 1) ExeggutorRearrange();
                time_elapse(42);
                Advance(1);    //Cry Inside Time Delay
                time_elapse(DelayTime - 43);
                return;
            }
            time_elapse(DelayTime);
        }

        private static int getframeshift()
        {
            if (Considerdelay)
                time_delay();
            else
                ResetModelStatus();

            if (igenerator is WildRNG) //Wild
            {
                ResetModelStatus();
                if (route17) Advance(2);
                time_elapse(1);              //Blink process also occurs when loading map
                index += PreHoneyCorrection - modelnumber;  //Pre-HoneyCorrection
                time_elapse(93);
            }
            return index;
        }

        // MainRNGEgg
        public static EggResult firstegg;
        #endregion
    }
}
