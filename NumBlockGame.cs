using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace numBlock
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class NumBlockGame : Microsoft.Xna.Framework.Game
    {
        private static int SCREEN_WIDTH = 480;
        private static int SCREEN_HEIGHT = 800;
        public static int BOARD_OFFSET_X = 46;
        public static int BOARD_OFFSET_Y = 150;
        public static int BOARD_OFFSET_SLOT_Y = 94;
        public static int REMOVE_ANIMATION_LENGTH = 550;
        public static int MAX_LOCAL_HIGH_SCORES = 10;

        public static int NUM_COLUMNS = 8;
        public static int NUM_ROWS = 8;

        public static Random rand = new Random();
        GraphicsDeviceManager graphics;
        public SpriteBatch spriteBatch;
        public SpriteFont font;
        Matrix worldToScreenMatrix;
        Rectangle worldRect;

        /*
         * Assets
         */
        Texture2D background;
        Texture2D splashScreen;
        Texture2D menuSelectIcon;
        Texture2D gameOver;
        Texture2D highScoresBg;
        Texture2D achieveScreenBg;
        Texture2D instructionsScreenBg;
        Texture2D checkMarkTexture;
        Texture2D pauseOverlay;
        Texture2D gameOverHighScoreOverlay;
        List<Texture2D> blockIcons = null;
        List<Texture2D> alphaTextures = null;
        SoundEffect blockLand = null;
        SoundEffect ping = null;
        SoundEffect achSound = null;
        public SoundEffect unlock = null;
        Texture2D charSelectArrows = null;
        public Texture2D achBG = null;
        public List<Texture2D> achIcons = null;
        public Texture2D achIconNone = null;
        
        /*
         * Gameplay attributes
         */
        public List<List<NumberBlock>> gameBoard;
        public KeyboardState prevKeyState;
        public MouseState prevMouseState;
        public List<ScoreSprite> scoreSprites;
        public List<AchievementSpriteComponent> achSpriteList;
        public AchievementHub achievementHub;
        public NumberBlock nextBlock;
        public List<NumberBlock> nextBlockQueue;
        public int nextBlockColumn = 0;

        public List<NumberBlock> blocksToRemove = null;
        StringBuilder comboString = null;

        private int droppingTimeout = 0;
        private int removingTimeout = 0;

        public int score = 0;
        public int multiplier = 1;
        public int comboScore = 0;
        public int comboRecord = 0;
        public int numMovesPerLevel = 0;
        public int numMovesLeft = 0;
        public int currLevel = 1;

        GameSettings settings;

        /*
         * UI Fields
         */
        public char[] initials = null;
        public int currInitial = 0;
        //public int mainMenuOptionIndex = 0;
        public int tempIndex = 1;
        bool saveHighScore = false;
        bool unfocused = false;
        //bool paused = false;
        //int mouseDownInitPos = 0;

        //Time delay quit
        int timeToQuit = -1;

        public enum AppState
        {
            MainMenu,
            Game,
            HighScores,
            Achievements,
            Instructions,
            Unfocused,
            Quiting
        }
        public AppState appState = AppState.MainMenu; //GameState.Game;
        public AppState previousAppState = AppState.MainMenu;

        public enum GamePlayState
        {
            idle,
            dropping,
            checking,
            removingAnim,
            removing,
            gameOver
        }
        public GamePlayState gamePlayState = GamePlayState.idle;
        public int dropBlockCount = 0;
        public numblock savedData;
        Menu mainMenu;
        Menu gameOverMenu;

        public NumBlockGame()
        {
            savedData = StoredInfo.LoadStoredData();
            comboRecord = Convert.ToInt32(savedData.playerinfo.combo);

            appState = AppState.MainMenu;
            scoreSprites = new List<ScoreSprite>();
            achSpriteList = new List<AchievementSpriteComponent>();
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = SCREEN_WIDTH;
            graphics.PreferredBackBufferHeight = SCREEN_HEIGHT;
            Content.RootDirectory = "Content/images";

            SoundEffect.MasterVolume = 0.3f;

            prevKeyState = Keyboard.GetState();
            prevMouseState = Mouse.GetState();

            blocksToRemove = new List<NumberBlock>();

            mainMenu = new Menu(100,new int[] { 310, 355, 400, 445, 502 });
            gameOverMenu = new Menu(120, new int[] { 318, 363 });

            initials = savedData.playerinfo.initials.ToCharArray();
            achievementHub = new AchievementHub(savedData.playerinfo.achievments);

            this.settings = new GameSettings();
            this.settings.applyDifficulty(GameSettings.Difficulty.Medium);
        }

        private void ClearGameBoard()
        {
            if (gameBoard != null)
            {
                foreach (List<NumberBlock> column in this.gameBoard)
                {
                    foreach (NumberBlock block in column)
                    {
                        Components.Remove(block);
                    }
                }
            }
            gameBoard = new List<List<NumberBlock>>();
            for (int i = 0; i < NUM_COLUMNS; i++)
            {
                gameBoard.Add(new List<NumberBlock>());
            }
        }

        private void generateNewGame()
        {
            this.comboString = new StringBuilder();
            this.saveHighScore = false;
            score = 0;
            currLevel = 1;
            numMovesPerLevel = GetNextNumBlocks();
            numMovesLeft = numMovesPerLevel;

            ClearGameBoard();
            int addElementProbability = this.settings.addElementProbability;

            for (int i = 0; i < NUM_COLUMNS; i++)
            {
                List<NumberBlock> column = gameBoard[i];
                for (int j = 0; j < NUM_ROWS; j++)
                {
                    if (rand.Next(100) <= addElementProbability)
                    {
                        NumberBlock newBlock = randomBlock(true);
                        newBlock.column = i;
                        newBlock.position = new Vector2((i * 48 + BOARD_OFFSET_X + i * 2), (BOARD_OFFSET_Y + j*48 + j*2));

                        column.Add(newBlock);
                        Components.Add(newBlock);
                    }
                }

                //Drop after adding elements, as the indexes can only be known after all blocks are dropped
                foreach (NumberBlock block in column)
                {
                    if (block.Drop())
                        this.dropBlockCount++;
                }
            }

            CreateNextBlock(true);

            //Create premonition blocks
            nextBlockQueue = new List<NumberBlock>();
            int numBlocksForseen = this.settings.numBlocksForseen;
            for (int i = 0; i < numBlocksForseen; i++)
            {
                nextBlockQueue.Add(randomBlock(false));
            }

            gamePlayState = GamePlayState.dropping;
        }

        private void CreateNextBlock(bool cleanQueue)
        {
            int total = 0;
            foreach (List<NumberBlock> col in this.gameBoard)
            {
                total += col.Count();
            }
            if (total == NUM_COLUMNS * NUM_ROWS)
            {
                GameOver();
            }
            else
            {
                if (total == 0)
                {
                    this.Score(new Vector2(BOARD_OFFSET_X + 5, BOARD_OFFSET_SLOT_Y + NUM_ROWS*50 - 20), this.currLevel * 20000);
                    if (achievementHub.hasAchieved(AchievementHub.ACH_CLEAN_SLATE) == false)
                    {
                        AwardAchievement(AchievementHub.ACH_CLEAN_SLATE);
                    }
                }

                if (cleanQueue == true || nextBlockQueue.Count() == 0)
                {
                    nextBlock = randomBlock(false);
                }
                else
                {
                    nextBlock = nextBlockQueue.ElementAt(0);
                    nextBlockQueue.RemoveAt(0);
                    nextBlockQueue.Add(randomBlock(false));    
                }
                
                nextBlock.position.X = BOARD_OFFSET_X + nextBlockColumn * 50;
                nextBlock.position.Y = BOARD_OFFSET_SLOT_Y;
                Components.Add(nextBlock);
            }
        }

        private void GameOver()
        {
            gamePlayState = GamePlayState.gameOver;
            if (isHighScore(this.score))
            {
                this.saveHighScore = true;
            }
        }

        private NumberBlock randomBlock(bool allowLockedBlocks)
        {
            NumberBlock returnBlock = new NumberBlock(this);

            if(allowLockedBlocks)
                returnBlock.blockNumber = rand.Next(0,9);
            else
                returnBlock.blockNumber = rand.Next(1, 9);

            if (returnBlock.blockNumber == 0)
            {
                returnBlock.lockCount = 2;
            }
            else if (returnBlock.blockNumber == 9)
            {
                returnBlock.lockCount = 1;
            }
            return returnBlock;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            worldRect = new Rectangle(0, 0, 272, 480);

            worldToScreenMatrix = Matrix.CreateScale(
                (float)GraphicsDevice.Viewport.Width / (float)worldRect.Width,
                (float)GraphicsDevice.Viewport.Height / (float)worldRect.Height,
                1);

            this.IsMouseVisible = true;

            base.Initialize();
        }

        private int GetNextNumBlocks()
        {
            //Bit too fast => 2*(1.5 + ArcTan[3-0.5x]) + 5
            //2*(1.5 + ArcTan[3-0.5(x-1.5)]) + 5
            int returnVal = 0;
            if (this.settings.difficulty == GameSettings.Difficulty.Easy)
            {
                returnVal = (int)(3 * (1.5 + Math.Atan(3 - 0.5 * (currLevel - 1.5))) + 8);
            }
            else if (this.settings.difficulty == GameSettings.Difficulty.Hard)
            {
                //returnVal = (int)(2 * (1.5 + Math.Atan(3 - 0.5 * currLevel)) + 5);
                //(2 * (1.5 + atan(3 - 0.5 * (x- 0.5))) + 3)
                returnVal = (int)(2 * (1.5 + Math.Atan(3 - 0.5 * (currLevel - 0.5))) + 3);
            }
            else
            {
                returnVal = (int)(2 * (1.5 + Math.Atan(3 - 0.5 * (currLevel - 1.5))) + 5);
            }

            return returnVal;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            blockIcons = new List<Texture2D>();
            for (int i = 0; i < 11; i++)
            {
                blockIcons.Add(Content.Load<Texture2D>("block_" + i));
            }

            alphaTextures = new List<Texture2D>();
            for (int i = 0; i < 26; i++)
            {
                alphaTextures.Add(Content.Load<Texture2D>("alpha" + i));
            }

            achBG = Content.Load<Texture2D>("ach_bg");
            achIcons = new List<Texture2D>();
            for (int i = 0; i < 7; i++)
            {
                achIcons.Add(Content.Load<Texture2D>("achIcon_" + i));
            }
            achIconNone = Content.Load<Texture2D>("achIcon_none");


            
            background = Content.Load<Texture2D>("background");
            splashScreen = Content.Load<Texture2D>("splash");
            menuSelectIcon = Content.Load<Texture2D>("menuSelect");
            gameOver = Content.Load<Texture2D>("gameOver");
            highScoresBg = Content.Load<Texture2D>("scores_bg");
            achieveScreenBg = Content.Load<Texture2D>("ach_screen_bg");
            instructionsScreenBg = Content.Load<Texture2D>("ins_screen_bg");
            charSelectArrows = Content.Load<Texture2D>("arrows");
            font = Content.Load<SpriteFont>("ScoreFont");
            checkMarkTexture = Content.Load<Texture2D>("checkmark");
            pauseOverlay = Content.Load<Texture2D>("paused");
            gameOverHighScoreOverlay = Content.Load<Texture2D>("gameOver_highScoreOverlay");

            blockLand = Content.Load<SoundEffect>("thunk2");
            unlock = Content.Load<SoundEffect>("unlock2");
            ping = Content.Load<SoundEffect>("ping2");
            achSound = Content.Load<SoundEffect>("achSound");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState(PlayerIndex.One);
            MouseState mouseState = Mouse.GetState();

            if (appState == AppState.MainMenu)
            {
                HandleMainMenuAppState(gameTime, keyState, mouseState);
            }
            else if(appState == AppState.Game)
            {
                HandlePlayAppState(gameTime, keyState, mouseState);
            }
            else if (appState == AppState.Unfocused)
            {
                HandleUnfocused(keyState, mouseState);
            }
            else if (appState == AppState.HighScores || appState == AppState.Achievements || appState == AppState.Instructions)
            {
                HandleHighScoresAppState(keyState, mouseState);
            }
            else if (appState == AppState.Quiting)
            {
                if (gameTime.TotalGameTime.Seconds > timeToQuit)
                    this.CloseGame(gameTime);
            }

            this.prevKeyState = keyState;
            this.prevMouseState = mouseState;
            base.Update(gameTime);
        }

        private void HandleUnfocused(KeyboardState keyState, MouseState mouseState)
        {
            if (unfocused)
                return;

            if (keyState.IsKeyDown(Keys.Enter) && prevKeyState.IsKeyDown(Keys.Enter) == false
              || mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed)
            {
                appState = previousAppState;
            }
        }

        private void HandleHighScoresAppState(KeyboardState keyState, MouseState mouseState)
        {
            if ((keyState.IsKeyDown(Keys.Enter) && prevKeyState.IsKeyDown(Keys.Enter) == false)
               || (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed)
               || (keyState.IsKeyDown(Keys.Escape)))
            {
                this.appState = AppState.MainMenu;
            }
        }

        private void HandlePlayAppState(GameTime gameTime, KeyboardState keyState, MouseState mouseState)
        {
            // Allows the game to exit
            if (keyState.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                appState = AppState.MainMenu;
                return;
            }

            if (gamePlayState == GamePlayState.idle)
            {
                HandleIdleState(keyState, mouseState);
            }
            else if (gamePlayState == GamePlayState.checking)
            {
                HandleCheckingState();
            }
            else if (gamePlayState == GamePlayState.removingAnim)
            {
                if (removingTimeout == 0)
                {
                    removingTimeout = 0;
                }
                removingTimeout += gameTime.ElapsedGameTime.Milliseconds;
                if (removingTimeout > GetRemoveAnimationTime())
                {
                    removingTimeout = 0;
                    gamePlayState = GamePlayState.removing;
                }
            }
            else if (gamePlayState == GamePlayState.removing)
            {

                HandleRemoveState();
            }
            else if (gamePlayState == GamePlayState.dropping)
            {
                HandleDroppingState(gameTime);
            }
            else if (gamePlayState == GamePlayState.gameOver)
            {
                HandleGameOver(keyState, mouseState);
            }
        }

        private void HandleMainMenuAppState(GameTime gameTime, KeyboardState keyState, MouseState mouseState)
        {            
            if (keyState.IsKeyDown(Keys.Escape) && prevKeyState.IsKeyDown(Keys.Escape) == false)
            {
                this.CloseGame(gameTime);
            }

            bool menuIndexChanged = mainMenu.HandleMenuInput(mouseState, prevMouseState, keyState, prevKeyState);
            if (menuIndexChanged)
                ping.Play();

            if (keyState.IsKeyDown(Keys.Enter) && prevKeyState.IsKeyDown(Keys.Enter) == false
                || (mainMenu.dragged == false && mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed))
            {
                if (mainMenu.menuOptionIndex == 0)
                {
                    appState = AppState.Game;
                    generateNewGame();
                }
                else if (mainMenu.menuOptionIndex == 1)
                {
                    appState = AppState.HighScores;
                }
                else if (mainMenu.menuOptionIndex == 2)
                {
                    appState = AppState.Achievements;
                }
                else if (mainMenu.menuOptionIndex == 3)
                {
                    appState = AppState.Instructions;
                }
                else if (mainMenu.menuOptionIndex == 4)
                {
                    this.CloseGame(gameTime);
                }
            }

        }

        private void CloseGame(GameTime gameTime)
        {
            if (achievementHub.hasAchieved(AchievementHub.ACH_QUIT_GAME) == false)
            {
                AwardAchievement(AchievementHub.ACH_QUIT_GAME);
                timeToQuit = gameTime.TotalGameTime.Seconds + 2;
                appState = AppState.Quiting;
                if (achievementHub.SaveAchievements(savedData.playerinfo))
                {
                    StoredInfo.SaveData(this.savedData);
                }
            }
            else
            {
                this.Exit();
            }
        }

        private void HandleGameOver(KeyboardState keyState, MouseState mouseState)
        {            
            if (saveHighScore == false)
            {
                bool menuIndexChanged = gameOverMenu.HandleMenuInput(mouseState, prevMouseState, keyState, prevKeyState);
                if (menuIndexChanged)
                    ping.Play();

                if (keyState.IsKeyDown(Keys.Enter) && prevKeyState.IsKeyDown(Keys.Enter) == false
                    || (gameOverMenu.dragged == false && mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed))
                {
                    //Save Data
                    if (achievementHub.SaveAchievements(savedData.playerinfo))
                    {
                        StoredInfo.SaveData(this.savedData);
                    }

                    //Handle Menu entry
                    if (gameOverMenu.menuOptionIndex == 0)
                    {
                        generateNewGame();
                    }
                    else if (gameOverMenu.menuOptionIndex == 1)
                    {
                        appState = AppState.MainMenu;
                    }
                }
            }
            else
            {
                if (keyState.IsKeyDown(Keys.Enter) && prevKeyState.IsKeyDown(Keys.Enter) == false 
                    || (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed))
                {
                    achievementHub.SaveAchievements(savedData.playerinfo);
                    SaveHighScore();
                }
                if (keyState.IsKeyDown(Keys.Left) && prevKeyState.IsKeyDown(Keys.Left) == false)                    
                {
                    //|| mouseState.RightButton == ButtonState.Pressed && prevMouseState.RightButton == ButtonState.Released)
                    if (this.currInitial > 0)
                    {
                        this.currInitial--;
                    }
                }

                if (keyState.IsKeyDown(Keys.Right) && prevKeyState.IsKeyDown(Keys.Right) == false)
                {
                    //|| mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
                    if (this.currInitial < 2)
                    {
                        this.currInitial++;
                    }
                }

                if (keyState.IsKeyDown(Keys.Up) && prevKeyState.IsKeyDown(Keys.Up) == false
                    || mouseState.ScrollWheelValue > prevMouseState.ScrollWheelValue)
                {
                    HandleUpInitial();
                }
                else if (keyState.IsKeyDown(Keys.Down) && prevKeyState.IsKeyDown(Keys.Down) == false
                    || mouseState.ScrollWheelValue < prevMouseState.ScrollWheelValue)
                {
                    HandleDownInitial();
                }
            }
        }

        private void SaveHighScore()
        {
            score newHighScore = new score();
            newHighScore.level = this.currLevel.ToString();
            newHighScore.value = this.score.ToString();
            newHighScore.initials = new String(this.initials);

            savedData.playerinfo.initials = newHighScore.initials;

            score[] localScores = this.savedData.highscores.local;
            score[] newLocalScores = null;
            int length = localScores.Count();
            if (length < MAX_LOCAL_HIGH_SCORES)
                newLocalScores = new score[length + 1];
            else
                newLocalScores = new score[MAX_LOCAL_HIGH_SCORES];

            bool indexfound = false;
            for (int i = 0; i < length; i++)
            {
                if (indexfound == false)
                {
                    if (Convert.ToInt64(localScores[i].value) < this.score)
                    {
                        indexfound = true;
                        newLocalScores[i] = newHighScore;
                    }
                    else
                    {
                        newLocalScores[i] = localScores[i];
                    }
                }
                else if(i < MAX_LOCAL_HIGH_SCORES-1)
                {
                    newLocalScores[i] = localScores[i-1];
                }
            }
            //Special condition, new score is lowest on list
            if (indexfound == false)
                newLocalScores[length] = newHighScore;

            //Special condition, new score is highest on list, yet list isn't full
            if (newLocalScores[newLocalScores.Count()-1] == null)
                newLocalScores[newLocalScores.Count()-1] = localScores[length - 1];

            this.savedData.highscores.local = newLocalScores;
            
            StoredInfo.SaveData(this.savedData);

            appState = AppState.HighScores;
        }

        private void HandleRemoveState()
        {
            foreach (NumberBlock block in blocksToRemove)
            {
                List<NumberBlock> col = this.gameBoard[block.column];

                /*
                 *  Check the locks on the block above
                 */
                int index = col.IndexOf(block);
                if (index > 0)
                {
                    col[index - 1].checkLock();
                }

                /*
                 * Check the locks on the block bellow
                 */
                if (index < col.Count()-1)
                {
                    col[index + 1].checkLock();
                }

                /*
                 * Check the locks on the block to the left
                 */
                int heightCheck = col.Count() - index;
                if (block.column > 0)
                {
                    List<NumberBlock> leftColumn = this.gameBoard[block.column - 1];
                    if (leftColumn.Count() >= heightCheck)
                    {
                        leftColumn[leftColumn.Count()-heightCheck].checkLock();
                    }
                }
                /*
                 * Check the locks on the block to the right
                 */
                if (block.column < NUM_COLUMNS-1)
                {
                    List<NumberBlock> rightColumn = this.gameBoard[block.column + 1];
                    if (rightColumn.Count() >= heightCheck)
                    {
                        rightColumn[rightColumn.Count() - heightCheck].checkLock();
                    }
                }

                col.RemoveAt(index);

                for (int i = 0; i < col.Count(); i++)
                {
                    NumberBlock curBlock = col[i];
                    if (curBlock.beingRemoved == false && col[i].Drop())
                        this.dropBlockCount++;
                }
            }

            multiplier++;
            gamePlayState = GamePlayState.dropping;
        }

        private void HandleDroppingState(GameTime gameTime)
        {
            foreach (NumberBlock rblock in blocksToRemove)
            {
                Components.Remove(rblock);
            }
            blocksToRemove = new List<NumberBlock>();

            /*
             * Catch conditions where blocks drop at the top of a stack
             * or manage to lock in dropping state
             */
            if (this.dropBlockCount <= 0)
            {
                this.dropBlockCount = 0;
            }
            else
            {
                if (droppingTimeout == 0)
                {
                    droppingTimeout = 0;
                }
                droppingTimeout += gameTime.ElapsedGameTime.Milliseconds;
                if (droppingTimeout > 750)
                    this.dropBlockCount = 0;
            }
            if (this.dropBlockCount == 0)
                gamePlayState = GamePlayState.checking;
        }

        private void HandleCheckingState()
        {
            droppingTimeout = 0;
            dropBlockCount = 0;
            int[] maxMatchIndex = new int[NUM_COLUMNS];

            //check for matches
            int i = 0;
            foreach (List<NumberBlock> column in this.gameBoard)
            {
                maxMatchIndex[i] = -1;

                int j = 0;
                foreach (NumberBlock block in column)
                {
                    bool matchFound = false;
                    if (block.lockCount > 0)
                    {
                        //do nothing
                    } else if (block.blockNumber == column.Count())
                    {
                        matchFound = true;
                    } else {
                        //TODO count contiguous row count
                        int heightCheck = column.Count() - j;
                        int leftCount = 0;
                        for (int l = i - 1; l >= 0; l--)
                        {
                            if (this.gameBoard[l].Count() >= heightCheck)
                                leftCount++;
                            else
                                break;
                        }

                        int rightCount = 0;
                        for (int l = i + 1; l < NUM_COLUMNS; l++)
                        {
                            if (this.gameBoard[l].Count() >= heightCheck)
                                rightCount++;
                            else
                                break;
                        }

                        if (leftCount + rightCount + 1 == block.blockNumber)
                        {
                            matchFound = true;
                        }
                    }

                    if(matchFound) {
                        maxMatchIndex[i] = j;
                        this.blocksToRemove.Add(block);
                        block.beingRemoved = true;
                        this.Score(block.position);
                        this.comboString.Append(block.blockNumber);
                    }
                    j++;
                }
                i++;
            }

            if (maxMatchIndex.Sum() > -1 * NUM_COLUMNS)
            {
                gamePlayState = GamePlayState.removingAnim;
            }
            else
            {
                this.comboString.Append("|");

                if (numMovesLeft == 0)
                {
                    currLevel++;
                    numMovesPerLevel = GetNextNumBlocks();
                    numMovesLeft = numMovesPerLevel;
                    PushNewRow();
                }
                else
                {
                    CheckComboString();
                    gamePlayState = GamePlayState.idle;
                }
            }
        }

        private void CheckComboString()
        {
            /*
             * Evaluate combo string for bonus matches and achievements
             */            
            String combo = this.comboString.ToString();

            if (achievementHub.hasAchieved(AchievementHub.ACH_3_to_1) == false)
            {
                Regex regX_321 = new Regex("3.*2.*1");
                if (regX_321.IsMatch(combo)) 
                {
                    AwardAchievement(AchievementHub.ACH_3_to_1);
                }
            }
            if (achievementHub.hasAchieved(AchievementHub.ACH_FINAL_COUNTDOWN) == false)
            {
                Regex regX_8to1 = new Regex("8.*7.*6.*5.*4.*3.*2.*1");
                if (regX_8to1.IsMatch(combo))
                {
                    AwardAchievement(AchievementHub.ACH_FINAL_COUNTDOWN);
                }
            }


            
            //Clear the Combo String
            comboString = new StringBuilder();
        }

        private void Score(Vector2 pos)
        {
            this.Score(pos, -1);
        }

        private void Score(Vector2 pos, int scoreVal)
        {
            //(float)((int)multiplier - 6.0f) / 6;
            float pitch = (float)((int)multiplier - 3.0f) / 6; //  / 5;
            if (pitch > 1.0f)
                pitch = 1.0f;
            int scoreMod = 0;

            if (scoreVal > 0)
            {
                scoreMod = scoreVal;
            }
            else
            {
                scoreMod = 10 * multiplier * multiplier + 30 * multiplier + 50;
                //only add points from removed blocks to combo value
                comboScore += scoreMod;
            }
            score += scoreMod;            

            ScoreSprite sprite = new ScoreSprite(this);
            sprite.StartScoreSprite(scoreMod, (int)(pos.X + 5), (int)(pos.Y+3));
            Components.Add(sprite);

            ping.Play(1.0f, pitch, 0.0f);
        }

        private void HandleIdleState(KeyboardState keyState, MouseState mouseState)
        {
            /*
             * Reset combo fields
             */
            multiplier = 1;            
            if (comboScore > comboRecord)
            {
                AwardAchievement(AchievementHub.ACH_COMBO_BREAKER);
                this.comboRecord = comboScore;
                this.savedData.playerinfo.combo = comboScore.ToString();
            }
            if (this.achievementHub.hasAchieved(AchievementHub.ACH_OVER_9000) == false && comboScore > 9000)
            {
                AwardAchievement(AchievementHub.ACH_OVER_9000);
            }
            comboScore = 0;

            if (nextBlock == null)
            {
                CreateNextBlock(false);         
            }

            /*
             * Handle Idle State
             */
            if (keyState.IsKeyDown(Keys.Space) && prevKeyState.IsKeyDown(Keys.Space) == false)
            {
                DropNextBlock();                
            }

            if (keyState.IsKeyDown(Keys.Left) && prevKeyState.IsKeyDown(Keys.Left) == false && nextBlockColumn > 0)
            {
                if (nextBlock != null)
                {
                    nextBlockColumn -= 1;
                    nextBlock.MoveLeft();
                }
            }

            if (keyState.IsKeyDown(Keys.Right) && prevKeyState.IsKeyDown(Keys.Right) == false && nextBlockColumn < 7)
            {
                if (nextBlock != null)
                {
                    nextBlockColumn += 1;
                    nextBlock.MoveRight();
                }
            }

            if (keyState.IsKeyDown(Keys.Enter) && prevKeyState.IsKeyDown(Keys.Enter) == false)
            {
                /*float step = 0.05f;
                float pitch = tempIndex++*step;
                if (pitch > 1.0)
                {
                    pitch = -0.0f;
                    tempIndex = 0;
                }
                    
                ping.Play(1.0f, pitch, 0.0f);*/
                //multiplier++;
                //this.Score(new Vector2());
                //GameOver();
                //AwardAchievement(AchievementHub.ACH_SURVIVOR_MAN);
            }

            if (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed)
            {
                DropNextBlock();
            }
            else if (mouseState.LeftButton == ButtonState.Pressed)
            {
                //Null hanlding for when M1 + spacebar are both held down
                if (nextBlock != null)
                {
                    int mouseOverColumn = (int)((mouseState.X - BOARD_OFFSET_X) / 50);
                    if (mouseOverColumn < 0)
                        mouseOverColumn = 0;
                    else if (mouseOverColumn > NUM_COLUMNS - 1)
                        mouseOverColumn = NUM_COLUMNS - 1;

                    nextBlock.position.X = BOARD_OFFSET_X + 50 * mouseOverColumn;

                    this.nextBlockColumn = mouseOverColumn;
                }
            }
        }

        private void DropNextBlock()
        {
            if (nextBlock != null)
            {
                if (gameBoard[nextBlockColumn].Count() != NUM_ROWS)
                {
                    gameBoard[nextBlockColumn].Insert(0, nextBlock);
                    nextBlock.column = nextBlockColumn;
                    this.numMovesLeft--;
                    nextBlock.Drop();
                    nextBlock = null;
                    this.dropBlockCount++;
                    this.gamePlayState = GamePlayState.dropping;
                }
            }
        }

        private void Mute()
        {
            //TODO manage volume levels so it doesn't just turn the volume up to 100%
            float volume = Math.Abs(SoundEffect.MasterVolume - 1);
            SoundEffect.MasterVolume = (float)volume;
        }

        // draws text with 1-pixel drop shadow
        private void DrawStringHelper(SpriteBatch batch, SpriteFont sfont, string text, int x, int y, Color color)
        {
            batch.DrawString(sfont, text, new Vector2(x + 1, y + 1), Color.Black);
            batch.DrawString(sfont, text, new Vector2(x, y), color);
        }

        private void DrawBlock(NumberBlock block, SpriteBatch spriteBatch)
        {
            Texture2D blockSprite = null;
            if (block.blockNumber == -1)
            {
                blockSprite = this.blockIcons[9];
            }
            else
            {
                blockSprite = this.blockIcons[block.blockNumber];
            }

            Color tint = Color.White;
            if (gamePlayState == GamePlayState.removingAnim && block.beingRemoved)
            {
                byte alpha = (byte)(256 - 256* removingTimeout / GetRemoveAnimationTime());
               tint = new Color(Color.Pink, alpha);
            }
            spriteBatch.Draw(blockSprite, block.position, tint);
        }

        private int GetRemoveAnimationTime()
        {
            return REMOVE_ANIMATION_LENGTH; //(int)(REMOVE_ANIMATION_LENGTH / Math.Ceiling((multiplier + 1.0d)/ 2));
        }

        public void StopDrop()
        {
            dropBlockCount--;
            blockLand.Play(0.2f,0.0f,0.0f);
        }

        public void PushNewRow()
        {
            int pushNonBrickProbability = this.settings.pushNonBrickProbability;

            int i = 0;
            foreach (List<NumberBlock> column in this.gameBoard)
            {
                int count = column.Count();
                if (count == NUM_ROWS)
                {
                    GameOver();
                }
                NumberBlock newBlock = null;
                if (rand.Next(100) <= pushNonBrickProbability)
                {
                    newBlock = randomBlock(false);
                }
                else
                {
                    newBlock = new NumberBlock(this);
                    newBlock.blockNumber = 0;
                    newBlock.lockCount = 2;
                }
                newBlock.column = i;
                Components.Add(newBlock);
                column.Insert(column.Count(), newBlock);

                int j = 0;
                int startHeight = (NUM_ROWS - count - 1) * 50 + BOARD_OFFSET_Y;
                foreach (NumberBlock block in column)
                {
                    block.position = new Vector2((BOARD_OFFSET_X + block.column * 50), startHeight + (j+1) * 50);
                    block.destination_y = startHeight + j * 50;
                    block.Float();
                    this.dropBlockCount++;
                    
                    j++;
                }

                i++;
            }
            this.Score(new Vector2(BOARD_OFFSET_X, BOARD_OFFSET_SLOT_Y + 5), this.currLevel * 1000);

            if (gamePlayState != GamePlayState.gameOver)
            {
                if (this.currLevel == 31 && achievementHub.hasAchieved(AchievementHub.ACH_SURVIVOR_MAN) == false)
                {
                    AwardAchievement(AchievementHub.ACH_SURVIVOR_MAN);
                }
                gamePlayState = GamePlayState.dropping;
            }
        }

        private void HandleUpInitial()
        {
            if (this.initials[currInitial] == 'Z')
            {
                this.initials[currInitial] = 'A';
            }
            else
            {
                this.initials[currInitial] = (char)(((int)this.initials[currInitial]) + 1);
            }
            this.blockLand.Play();
        }

        private void HandleDownInitial()
        {
            if (this.initials[currInitial] == 'A')
            {
                this.initials[currInitial] = 'Z';
            }
            else
            {
                this.initials[currInitial] = (char)(((int)this.initials[currInitial]) - 1);
            }
            this.blockLand.Play();
        }

        private Texture2D GetTextureForChar(char c)
        {
            return this.alphaTextures[((int)c) - ((int)'A')];
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);

            spriteBatch.Begin();

            AppState drawState = appState;
            if (appState == AppState.Unfocused)
                drawState = previousAppState;

            if (drawState == AppState.Game)
            {
                DrawGameBoard(gameTime);
            }
            else if (drawState == AppState.MainMenu)
            {
                spriteBatch.Draw(splashScreen, new Vector2(0, 0), Color.White);
                Vector2 iconPoint = mainMenu.GetMenuIndexVector();
                spriteBatch.Draw(menuSelectIcon, iconPoint, Color.White);
            }
            else if (drawState == AppState.HighScores)
            {
                DrawHighScores();
            }
            else if (drawState == AppState.Achievements)
            {
                DrawAchievements();
            }
            else if (drawState == AppState.Instructions)
            {
                //Simple screen
                spriteBatch.Draw(instructionsScreenBg, Vector2.Zero, Color.White);
            }
            else if (drawState == AppState.Quiting)
            {
                //Draw quiting achievement if it exists
                foreach (AchievementSpriteComponent achSprite in achSpriteList)
                {
                    achSprite.Draw();
                }
            }
            
            if (appState == AppState.Unfocused) //breaks the mold
            {
                if (previousAppState == AppState.Game)
                {
                    DrawGameBoard(gameTime);
                }
                //DrawStringHelper(spriteBatch, font, "{click to continue}", 170, 300, Color.White);
                spriteBatch.Draw(pauseOverlay, Vector2.Zero, Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawAchievements()
        {
            spriteBatch.Draw(achieveScreenBg, new Vector2(0, 0), Color.White);

            Color aplhaBlend = new Color(Color.White.ToVector3());
            aplhaBlend.A = (byte)100;
            int count = 0;
            foreach (KeyValuePair<int, Achievement> pair in achievementHub.library)
            {
                Achievement ach = pair.Value;
                Vector2 position = new Vector2(113, 115 + 85 * count);
                Color c = aplhaBlend;

                Texture2D currAchIcon = achIconNone;
                if (ach.achieved)
                {
                    currAchIcon = achIcons[ach.achId];
                    c = Color.White;
                    spriteBatch.Draw(checkMarkTexture, new Vector2(position.X - 35, position.Y + 24), c);
                }

                spriteBatch.Draw(achBG, position, c);
                spriteBatch.Draw(currAchIcon, new Vector2(position.X + 7, position.Y + 7), c);
                spriteBatch.DrawString(font, ach.title, new Vector2(position.X + 78, position.Y + 7), c);
                spriteBatch.DrawString(font, ach.description, new Vector2(position.X + 78, position.Y + 30), c, 0.0f, new Vector2(), 0.7f, SpriteEffects.None, 0.0f);

                count++;
            }
        }

        private void DrawHighScores()
        {
            int LETTER_SPRITE_HIEGHT = 28;

            spriteBatch.Draw(highScoresBg, new Vector2(0, 0), Color.White);

            score[] localScores = this.savedData.highscores.local;
            int length = localScores.Count();
            for (int i = 0; i < length; i++)
            {
                score curScore = localScores[i];
                String drawString = Convert.ToInt64(curScore.value).ToString("#,##0");
                //DrawStringHelper(spriteBatch, font, drawString, BOARD_OFFSET_X + 148, BOARD_OFFSET_Y + LETTER_SPRITE_HIEGHT * i + 8, new Color(96,96,96));
                //DrawStringHelper(spriteBatch, font, curScore.level, BOARD_OFFSET_X + 298, BOARD_OFFSET_Y + LETTER_SPRITE_HIEGHT * i + 8, new Color(40, 40, 40));
                spriteBatch.DrawString(font, drawString, new Vector2(BOARD_OFFSET_X + 148, BOARD_OFFSET_Y + LETTER_SPRITE_HIEGHT * i + 8), new Color(0, 59, 255, 120));
                spriteBatch.DrawString(font, curScore.level, new Vector2(BOARD_OFFSET_X + 298, BOARD_OFFSET_Y + LETTER_SPRITE_HIEGHT * i + 8), new Color(119, 0, 255, 120));              

                for (int j = 0; j < curScore.initials.Length; j++)
                {
                    spriteBatch.Draw(GetTextureForChar(curScore.initials[j]), new Vector2(BOARD_OFFSET_X + j * 25 + 68, BOARD_OFFSET_Y + LETTER_SPRITE_HIEGHT * i + 8), null, Color.White, 0.0f, new Vector2(0, 0), 0.5f, SpriteEffects.None, 0.0f);
                }
            }

            if (this.comboRecord > AchievementHub.MIN_COMBO_RECORD)
            {
                spriteBatch.DrawString(font, "Highest Combo:   " + this.comboRecord.ToString("#,##0"), new Vector2(BOARD_OFFSET_X + 95, BOARD_OFFSET_Y + LETTER_SPRITE_HIEGHT * 10 + 18), new Color(255, 0, 0, 120));//Color.DarkSlateGray); //Color.Black);
            }
        }

        private void DrawGameBoard(GameTime gameTime)
        {
            spriteBatch.Draw(background, new Vector2(0, 0), Color.White);

            foreach (List<NumberBlock> column in this.gameBoard)
            {
                foreach (NumberBlock block in column)
                {
                    DrawBlock(block, spriteBatch);
                }
            }

            if (nextBlock != null)
                DrawBlock(nextBlock, spriteBatch);

            DrawStringHelper(spriteBatch, font, "Score:  " + score.ToString("#,##0"), BOARD_OFFSET_X-3, BOARD_OFFSET_Y - 87, Color.White);
            if(comboScore > 0)
                DrawStringHelper(spriteBatch, font, "+" + comboScore.ToString("#,##0"), BOARD_OFFSET_X + 47, BOARD_OFFSET_Y - 65, Color.Orange);

            DrawStringHelper(spriteBatch, font, "Level " + currLevel, BOARD_OFFSET_X + 50 * NUM_COLUMNS - 50, BOARD_OFFSET_Y - 87, Color.White);


            //Number of moves left
            //DrawStringHelper(spriteBatch, font, numMovesLeft+" / "+numMovesPerLevel, BOARD_OFFSET_X, BOARD_OFFSET_Y + 50*NUM_COLUMNS + 20, Color.White);
            Color tint = new Color(Color.White, 20);
            for (int i = 0; i < numMovesPerLevel; i++)
            {
                if (i > numMovesLeft - 1)
                {
                    spriteBatch.Draw(blockIcons[10], new Vector2(BOARD_OFFSET_X - 37, BOARD_OFFSET_SLOT_Y - 6 + i * 26), tint);
                }
                else if (i < nextBlockQueue.Count())
                {
                    NumberBlock queuedBlock = nextBlockQueue.ElementAt(i);
                    spriteBatch.Draw(blockIcons[queuedBlock.blockNumber], new Vector2(BOARD_OFFSET_X - 37, BOARD_OFFSET_SLOT_Y - 6 + i * 26), null, Color.White, 0.0f, new Vector2(), 0.5f, SpriteEffects.None, 0.0f);
                }
                else
                {
                    spriteBatch.Draw(blockIcons[10], new Vector2(BOARD_OFFSET_X - 37, BOARD_OFFSET_SLOT_Y - 6 + i * 26), Color.White);
                }
            }

            /*
             * Draw score sprites
             */
            foreach (ScoreSprite scoreSprite in scoreSprites)
            {
                Color scoreColor = new Color(Color.White.ToVector3());
                scoreColor.A = (byte)scoreSprite.alpha;
                spriteBatch.DrawString(font, "+" + scoreSprite.score.ToString("#,##0"), scoreSprite.position, scoreColor);
            }

            /*
             * Draw Achievement Sprites
             */
            foreach (AchievementSpriteComponent achSprite in achSpriteList)
            {
                achSprite.Draw();
            }

            /*
             * If Game over, draw overlay
             */
            if (gamePlayState == GamePlayState.gameOver)
            {
                if(this.saveHighScore)
                    spriteBatch.Draw(gameOverHighScoreOverlay, Vector2.Zero, Color.White);
                else
                    spriteBatch.Draw(gameOver, Vector2.Zero, Color.White);

                DrawStringHelper(spriteBatch, font, "Score:  " + score.ToString("#,##0"), BOARD_OFFSET_X + 152, BOARD_OFFSET_Y + 98, Color.OrangeRed);
                DrawStringHelper(spriteBatch, font, "Level:  " + currLevel, BOARD_OFFSET_X + 152, BOARD_OFFSET_Y + 128, Color.Gray);

                if (this.saveHighScore)
                {
                    //Draw initials
                    for (int i = 0; i < this.initials.Count(); i++)
                    {
                        spriteBatch.Draw(GetTextureForChar(initials[i]), new Vector2(184 + i * 50, 385), Color.White);
                    }
                    spriteBatch.Draw(charSelectArrows, new Vector2(193 + currInitial * 50, 337), Color.White);
                }
                else
                {
                    Vector2 iconPoint = gameOverMenu.GetMenuIndexVector();
                    spriteBatch.Draw(menuSelectIcon, iconPoint, Color.White);
                }
            }
        }

        private void AwardAchievement(int achId)
        {
            achievementHub.addAchievement(achId);
            AchievementSpriteComponent sprite = new AchievementSpriteComponent(this);
            sprite.display(achievementHub.getAchievement(achId));
            achSound.Play();
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            unfocused = false;
            //pulling this out to absorb click
            //appState = previousAppState;
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            unfocused = true;
            if(appState != AppState.Unfocused)
                previousAppState = appState;

            appState = AppState.Unfocused;
            base.OnDeactivated(sender, args);
        }

        private bool isHighScore(int value)
        {
            score[] localScores = savedData.highscores.local;
            foreach(score highScore in localScores) {
                if (Convert.ToInt64(highScore.value) < value)
                    return true;
            }
            return (localScores.Count() < MAX_LOCAL_HIGH_SCORES);
        }
    }
}
