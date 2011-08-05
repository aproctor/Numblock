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
    public class NumberBlock : Microsoft.Xna.Framework.GameComponent
    {
        public Guid guid;
        public int blockNumber = 0;
        public int lockCount = 0;
        public int column = 0;
        public bool beingRemoved = false;

        private static float ACCELERATION = 2500.0f;
        public Vector2 position;        
        public Vector2 velocity;
        public bool dropping = false;
        public bool floating = false;
        public int destination_y = 0;

        public NumberBlock(Game game)
            : base(game)
        {
            position = new Vector2(0, 0);
            velocity = new Vector2(0, 0);
            guid = System.Guid.NewGuid();
        }

        public void MoveLeft()
        {
            position.X -= 50;
        }

        public void MoveRight()
        {
            position.X += 50;
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
            if (dropping)
            {
                if (position.Y >= destination_y)
                {
                    dropping = false;
                    velocity.Y = 0;
                    position.Y = destination_y;
                    ((NumBlockGame)this.Game).StopDrop();
                }
                else
                {
                    velocity.Y += ACCELERATION * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    position.Y += velocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
            }
            else if (floating)
            {
                if (position.Y <= destination_y)
                {
                    this.floating = false;
                    velocity.Y = 0;
                    position.Y = destination_y;
                    ((NumBlockGame)this.Game).dropBlockCount--;
                }
                else
                {
                    velocity.Y += -500.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;//-2500.0f *(float)gameTime.ElapsedGameTime.TotalSeconds;
                    position.Y += velocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
            }

            base.Update(gameTime);
        }

        public bool BoostDrop()
        {
            this.velocity.Y = -200.0f;
            return this.Drop();
        }

        public bool Float()
        {
            this.floating = true;
            return true;
        }

        public bool Drop()
        {
            this.dropping = true;
            
            //TODO determine actual drop position
            List<NumberBlock> col = ((NumBlockGame)this.Game).gameBoard[this.column];
            int index = (col.Count()-1) - col.IndexOf(this);
            
            //TODO finish
            this.destination_y = NumBlockGame.BOARD_OFFSET_Y + (7-index)*48 + (7-index)*2;

            if (this.destination_y == this.position.Y)
            {
                this.dropping = false;
            }
            return this.dropping;
        }

        public override String ToString()
        {
            String returnVal = "[" + blockNumber + "]";
            return returnVal;
        }

        public override bool Equals(object obj)
        {
            return (obj is NumberBlock && this.guid.Equals(((NumberBlock)obj).guid));
        }

        public override int GetHashCode()
        {
            return this.guid.GetHashCode();
        }



        public void checkLock()
        {
            if (this.lockCount > 0)
            {
                ((NumBlockGame)this.Game).unlock.Play();
                this.lockCount--;
                if (this.lockCount == 0)
                {
                    this.blockNumber = NumBlockGame.rand.Next(1, 8);
                }
                else
                {
                    //Go to semi-unlocked sprite
                    this.blockNumber = 9;
                }
            }
        }
    }
}