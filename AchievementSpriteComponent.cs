using System;
using System.Collections.Generic;
using System.Linq;
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
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class AchievementSpriteComponent : Microsoft.Xna.Framework.GameComponent
    {
        Achievement achievement = null;
        public Guid guid;
        public static int TIME_ALIVE = 2350;
        public Vector2 position;
        public int timeToLive = 0;
        public int score = 0;
        public int alpha = 255;

        public AchievementSpriteComponent(Game game)
            : base(game)
        {
            guid = System.Guid.NewGuid();
            timeToLive = TIME_ALIVE;
            position = new Vector2();
        }

        public void display(Achievement ach)
        {
            List<AchievementSpriteComponent> spriteList = ((NumBlockGame)this.Game).achSpriteList;

            this.achievement = ach;
            this.position.X = 118;
            this.position.Y = 200 + 85 * spriteList.Count();
            
            spriteList.Add(this);
            this.Game.Components.Add(this);
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            int velocity = 0; //-60;
            timeToLive -= gameTime.ElapsedGameTime.Milliseconds;
            this.position.Y = this.position.Y + (int)(gameTime.ElapsedGameTime.TotalSeconds * velocity);

            alpha = (int)((((float)timeToLive) / TIME_ALIVE) * 255);
            if (timeToLive <= 0)
            {
                ((NumBlockGame)this.Game).achSpriteList.Remove(this);
                this.Game.Components.Remove(this);
            }
            base.Update(gameTime);
        }

        public void Draw()
        {
            Color c = new Color(Color.White.ToVector3());
            //c.A = (byte)this.alpha;
            NumBlockGame numGame = ((NumBlockGame)this.Game);

            numGame.spriteBatch.Draw(numGame.achBG, this.position, c);
            numGame.spriteBatch.Draw(numGame.achIcons[this.achievement.achId], new Vector2(this.position.X + 7, this.position.Y + 7), c);
            numGame.spriteBatch.DrawString(numGame.font, this.achievement.title, new Vector2(this.position.X + 78, this.position.Y + 7), c);
            numGame.spriteBatch.DrawString(numGame.font, this.achievement.description, new Vector2(this.position.X + 78, this.position.Y + 30), c, 0.0f, new Vector2(), 0.7f, SpriteEffects.None, 0.0f);
        }

        public override bool Equals(object obj)
        {
            return (obj is AchievementSpriteComponent && this.guid.Equals(((AchievementSpriteComponent)obj).guid));
        }

        public override int GetHashCode()
        {
            return this.guid.GetHashCode();
        }
    }
}