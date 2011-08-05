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
    public class ScoreSprite : Microsoft.Xna.Framework.GameComponent
    {
        public Guid guid;
        public static int TIME_ALIVE = 750;
        public Vector2 position;
        public int timeToLive = 0;
        public int score = 0;
        public int alpha = 255;

        public ScoreSprite(Game game)
            : base(game)
        {
            guid = System.Guid.NewGuid();
            timeToLive = TIME_ALIVE;
            position = new Vector2();
        }

        public void StartScoreSprite(int _score, int _x, int _y)
        {
            this.score = _score;
            this.position.X = _x;
            this.position.Y = _y;
            ((NumBlockGame)this.Game).scoreSprites.Add(this);
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
        public override void Update(GameTime gameTime)
        {
            int velocity = -100;
            timeToLive -= gameTime.ElapsedGameTime.Milliseconds;
            this.position.Y = this.position.Y + (int)(gameTime.ElapsedGameTime.TotalSeconds * velocity);

            alpha = (int)((((float)timeToLive) / TIME_ALIVE) * 255);
            if (timeToLive <= 0)
            {
                ((NumBlockGame)this.Game).scoreSprites.Remove(this);
                this.Game.Components.Remove(this);
            }
            base.Update(gameTime);
        }

        public override bool Equals(object obj)
        {
            return (obj is ScoreSprite && this.guid.Equals(((ScoreSprite)obj).guid));
        }

        public override int GetHashCode()
        {
            return this.guid.GetHashCode();
        }
    }
}