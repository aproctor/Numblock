using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace numBlock
{
    public class GameSettings
    {
        public enum Difficulty
        {
            Easy,
            Medium,
            Hard
        }

        public int addElementProbability = 20;
        public int pushNonBrickProbability = 35;
        public Difficulty difficulty;
        public int numBlocksForseen = 2;
        

        public GameSettings()
        {
            applyDifficulty(Difficulty.Medium);
        }

        public void applyDifficulty(Difficulty diff)
        {
            this.difficulty = diff;
            if (diff == Difficulty.Easy)
            {
                this.addElementProbability = 20;
                this.pushNonBrickProbability = 60;
                this.numBlocksForseen = 6;
            }
            else if (diff == Difficulty.Medium)
            {
                this.addElementProbability = 20;
                this.pushNonBrickProbability = 35;
                this.numBlocksForseen = 4;
            }
            else if (diff == Difficulty.Hard)
            {
                this.addElementProbability = 30;
                this.pushNonBrickProbability = 35;
                this.numBlocksForseen = 2;
            }
        }
    }
}
