using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace numBlock
{
    class Menu
    {
        public int menuOptionIndex;
        public int[] indexes;

        int x = 0;
        int mouseDownInitPos = 0;
        public bool dragged = false; 

        public Menu(int x_offset,int[] optionIndexes)
        {
            x = x_offset;
            menuOptionIndex = 0;
            indexes = optionIndexes;
        }


        public bool HandleMenuInput(MouseState mouseState, MouseState prevMouseState, KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool playPing = false;
            int MAX_MENU_INDEX = indexes.Count() - 1;

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                /*
                 * Check the drag height
                 */
                if (prevMouseState.LeftButton == ButtonState.Released)
                {
                    dragged = false;
                    mouseDownInitPos = mouseState.Y;
                }
                else
                {
                    int indexChangeAmount = (int)((mouseState.Y - mouseDownInitPos) / 45);
                    int newIndex = menuOptionIndex + indexChangeAmount;
                    if (newIndex < 0)
                        newIndex = 0;
                    else if (newIndex > MAX_MENU_INDEX)
                        newIndex = MAX_MENU_INDEX;

                    if (newIndex != menuOptionIndex)
                    {
                        dragged = true;
                        mouseDownInitPos = mouseState.Y;
                        menuOptionIndex = newIndex;
                        playPing = true;
                    }
                }
            } else {
                if ((keyState.IsKeyDown(Keys.Up) && prevKeyState.IsKeyDown(Keys.Up) == false || mouseState.ScrollWheelValue > prevMouseState.ScrollWheelValue) && menuOptionIndex > 0)
                {
                    menuOptionIndex--;
                    playPing = true;
                }
                if ((keyState.IsKeyDown(Keys.Down) && prevKeyState.IsKeyDown(Keys.Down) == false || mouseState.ScrollWheelValue < prevMouseState.ScrollWheelValue) && menuOptionIndex < MAX_MENU_INDEX)
                {
                    menuOptionIndex++;
                    playPing = true;
                }
            }

            return playPing;
        }

        public Vector2 GetMenuIndexVector()
        {
            return new Vector2(this.x, this.indexes[menuOptionIndex]);
        }
    }
}
