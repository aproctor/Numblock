using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace numBlock
{    
    public class Achievement
    {
        public int achId = 0;
        public String title;
        public String description;
        public bool achieved = false;

        public Achievement(int _achId, String _title, String _description)
        {
            achId = _achId;
            title = _title;
            description = _description;
        }


        public override bool Equals(object obj)
        {
            return (((Achievement)obj).achId == this.achId);
        }

        public override int GetHashCode()
        {
            return achId;
        }
    }

    public class AchievementHub
    {
        public static int ACH_CLEAN_SLATE = 0;
        public static int ACH_FINAL_COUNTDOWN = 1;
        public static int ACH_3_to_1 = 2;
        public static int ACH_OVER_9000 = 3;
        public static int ACH_COMBO_BREAKER = 4;
        public static int ACH_SURVIVOR_MAN = 5;
        public static int ACH_QUIT_GAME = 6;

        public static int MIN_COMBO_RECORD = 2000;

        List<Achievement> newAchievements = null;
        public Dictionary<int,Achievement> library = null;

        public AchievementHub(achievment[] savedDataList)
        {
            newAchievements = new List<Achievement>();
            GenerateLibrary(savedDataList);
        }

        private void GenerateLibrary(achievment[] savedDataList)
        {
            library = new Dictionary<int, Achievement>();

            /*
             * Declare all achievements
             * (try to inject a new line around 30 characters)
             */
            Achievement cleanSlate = new Achievement(ACH_CLEAN_SLATE, "Clean Slate", "Clear the game board of all blocks");
            library.Add(ACH_CLEAN_SLATE, cleanSlate);

            Achievement finalCountdown = new Achievement(ACH_FINAL_COUNTDOWN, "Final Countdown!", "Create a combo clearing all \nnumbers from 8 to 1");
            library.Add(ACH_FINAL_COUNTDOWN, finalCountdown);

            Achievement threeToOne = new Achievement(ACH_3_to_1, "Countdown", "Create a combo clearing the \nnumbers 3 2 and 1 in order");
            library.Add(ACH_3_to_1, threeToOne);

            Achievement over9000 = new Achievement(ACH_OVER_9000, "Over 9000", "Create a combo for more than \n9000 points");
            library.Add(ACH_OVER_9000, over9000);

            Achievement comboBreaker = new Achievement(ACH_COMBO_BREAKER, "Combo Breaker", "Break your highest combo record\nmust be greater than "+MIN_COMBO_RECORD+"");
            library.Add(ACH_COMBO_BREAKER, comboBreaker);

            Achievement survivorMan = new Achievement(ACH_SURVIVOR_MAN, "Surivor Man", "Last more than 30 levels");
            library.Add(ACH_SURVIVOR_MAN, survivorMan);

            Achievement quitGame = new Achievement(ACH_QUIT_GAME, "You'll be back", "...");
            library.Add(ACH_QUIT_GAME, quitGame);

            /*
             * Flag Achieved
             */
            foreach (achievment savedAch in savedDataList)
            {

                Achievement ach = null;
                library.TryGetValue(Convert.ToInt32(savedAch.ach_id), out ach);
                if (ach != null)
                {
                    ach.achieved = true;
                }
            }
        }

        public bool hasAchieved(int aId)
        {
            Achievement ach = null;
            library.TryGetValue(aId, out ach);
            if (ach != null)
                return ach.achieved;
            return false;
        }

        public void addAchievement(int aId)
        {
            Achievement ach = null;
            library.TryGetValue(aId, out ach);
            if (ach != null)
            {
                ach.achieved = true;
                newAchievements.Add(ach);
            }
        }

        private void addNewAchievementsToStoredData(playerinfo storedData)
        {
            int curLength = storedData.achievments.Count();
            int newAchLength = newAchievements.Count();
            achievment[] newStoredArray = new achievment[curLength + newAchLength];

            for (int i = 0; i < curLength; i++)
            {
                newStoredArray[i] = storedData.achievments[i];
            }
            for (int i = 0; i < newAchLength; i++)
            {
                Achievement ach = (Achievement)this.newAchievements.ElementAt(i);
                achievment newAch = new achievment();
                newAch.ach_id = ach.achId.ToString();
                newStoredArray[curLength + i] = newAch;
            }

            storedData.achievments = newStoredArray;

            //Clear the list for new achievements
            newAchievements = new List<Achievement>();
        }

        public bool SaveAchievements(playerinfo storedData)
        {
            bool requiresSave = newAchievements.Count() > 0;

            if (requiresSave)
                addNewAchievementsToStoredData(storedData);

            return requiresSave;
        }

        internal Achievement getAchievement(int p)
        {
            Achievement ach = null;
            library.TryGetValue(p, out ach);
            return ach;
        }
    }
}
