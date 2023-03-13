
using static SDL2.SDL;

public class GridItem
{
    public int X { get; set; }
    public int Y { get; set; }
    public SDL2.SDL.SDL_Color Color { get; set; }
}


public interface ITetrimino
{
    List<GridItem> GridItems { get; }
    SDL_Color Color { get; }
    int X { get; set; }
    int Y { get; set; }
    void Rotate();
}


public class Gameboard
{
    public const int Width = 10;
    public const int Height = 20;

    //Placed Items will be stored here
    public List<GridItem> GridItems { get; set; } = new();
    public ITetrimino FallingItem { get; set; }

    public Gameboard()
    {
        FallingItem = TetriminoFactory.CreateTetrimino();
    }

    public void Render(IntPtr renderer)
    {

        //Render the falling item
        foreach (var gridItem in FallingItem.GridItems)
        {
            var color = FallingItem.Color;
            //Render the grid item with a 3px border
            SDL_SetRenderDrawColor(renderer,
                color.r,
                color.g,
                color.b,
                color.a);
            SDL_Rect rect = new SDL_Rect()
            {
                x = (gridItem.X + FallingItem.X) * 32 + 3,
                y = (gridItem.Y + FallingItem.Y) * 32 + 3,
                w = 26,
                h = 26
            };
            SDL_RenderFillRect(renderer, ref rect);
        }

        foreach (var gridItem in GridItems)
        {
            //Render the grid item with a 3px border
            SDL_SetRenderDrawColor(renderer, gridItem.Color.r, gridItem.Color.g, gridItem.Color.b, gridItem.Color.a);
            SDL_Rect rect = new SDL_Rect()
            {
                x = gridItem.X * 32 + 3,
                y = gridItem.Y * 32 + 3,
                w = 26,
                h = 26
            };
            SDL_RenderFillRect(renderer, ref rect);
        }
    }
}

public class Game
{

    public Gameboard Gameboard { get; set; } = new();

    public void HandleKeyPress(SDL_Keycode key)
    {
        switch (key)
        {
            case SDL_Keycode.SDLK_LEFT:
                Gameboard.FallingItem.X--;
                HandleEdgeColisions();
                break;
            case SDL_Keycode.SDLK_RIGHT:
                Gameboard.FallingItem.X++;
                HandleEdgeColisions();
                break;
            case SDL_Keycode.SDLK_DOWN:
                lastFallTime = 0;
                break;
            case SDL_Keycode.SDLK_UP:
                Gameboard.FallingItem.Rotate();
                break;
            case SDL_Keycode.SDLK_SPACE:
                break;
        }
    }

    const int FallDelayMs = 1000;
    long lastFallTime = 0;



    public void Update(long deltaTimeMs, long tickMs)
    {

        if (tickMs - lastFallTime <= FallDelayMs) return;
        lastFallTime = tickMs;

        //If the falling item is at the bottom, place it on the gameboard
        if (Gameboard.FallingItem.Y + Gameboard.FallingItem.GridItems.Max(x => x.Y) >= Gameboard.Height - 1)
        {
            foreach (var gridItem in Gameboard.FallingItem.GridItems)
            {
                Gameboard.GridItems.Add(new GridItem()
                {
                    X = gridItem.X + Gameboard.FallingItem.X,
                    Y = gridItem.Y + Gameboard.FallingItem.Y,
                    Color = Gameboard.FallingItem.Color
                });
            }
            Gameboard.FallingItem = TetriminoFactory.CreateTetrimino();
        }
        else
        {
            Gameboard.FallingItem.Y++;
            HandleColisions();
        }

        if (CheckForFullRows(out int[] rowsToRemove))
        {
            RemoveRows(rowsToRemove);
        }

        HandleEdgeColisions();

        if (Gameboard.GridItems.Any(x => x.Y == 0))
        {
            //Game over
        }
    }

    private void HandleEdgeColisions()
    {
        //Check if the falling item is colliding with the left or right edge
        foreach (var gridItem in Gameboard.FallingItem.GridItems)
        {
            if (gridItem.X + Gameboard.FallingItem.X < 0)
            {
                Gameboard.FallingItem.X++;
            }
            else if (gridItem.X + Gameboard.FallingItem.X >= Gameboard.Width)
            {
                Gameboard.FallingItem.X--;
            }
        }

        //Check if the falling item is colliding with the top edge
        if (Gameboard.FallingItem.GridItems.Any(x => x.Y + Gameboard.FallingItem.Y < 0))
        {
            Gameboard.FallingItem.Y++;
        }

        //Check if the falling item is colliding with the bottom edge
        if (Gameboard.FallingItem.GridItems.Any(x => x.Y + Gameboard.FallingItem.Y >= Gameboard.Height))
        {
            Gameboard.FallingItem.Y--;
        }


    }

    private void RemoveRows(int[] rowsToRemove)
    {
        //Remove the rows
        foreach (var row in rowsToRemove)
        {
            Gameboard.GridItems.RemoveAll(x => x.Y == row);
        }

        //Move the rows above the removed rows down
        foreach (var row in rowsToRemove)
        {
            foreach (var gridItem in Gameboard.GridItems.Where(x => x.Y < row))
            {
                gridItem.Y++;
            }
        }
    }

    private bool CheckForFullRows(out int[] rowsToRemove)
    {
        rowsToRemove = new int[0];
        var rows = Gameboard.GridItems.GroupBy(x => x.Y).OrderBy(x => x.Key).ToList();
        var rowsToRemoveList = new List<int>();
        foreach (var row in rows)
        {
            if (row.Count() == Gameboard.Width)
            {
                rowsToRemoveList.Add(row.Key);
            }
        }

        if (rowsToRemoveList.Count > 0)
        {
            rowsToRemove = rowsToRemoveList.ToArray();
            return true;
        }

        return false;
    }

    private void HandleColisions()
    {
        //Check if the falling item is colliding with the gameboard
        foreach (var gridItem in Gameboard.FallingItem.GridItems)
        {
            if (Gameboard.GridItems.Any(x => x.X == gridItem.X + Gameboard.FallingItem.X && x.Y == gridItem.Y + Gameboard.FallingItem.Y))
            {
                //If the falling item is colliding, move it back up
                Gameboard.FallingItem.Y--;

                //Copy the items to the gameboard
                foreach (var fallingItemGridItem in Gameboard.FallingItem.GridItems)
                {
                    Gameboard.GridItems.Add(new GridItem()
                    {
                        X = fallingItemGridItem.X + Gameboard.FallingItem.X,
                        Y = fallingItemGridItem.Y + Gameboard.FallingItem.Y,
                        Color = Gameboard.FallingItem.Color
                    });
                }

                //Create a new falling item
                Gameboard.FallingItem = TetriminoFactory.CreateTetrimino();
                break;
            }
        }
    }

    internal void Render(nint renderer)
    {
        Gameboard.Render(renderer);
    }
}

public static class TetriminoFactory
{

    public class TetriminoI : ITetrimino
    {
        public List<GridItem> GridItems { get; } = new()
        {
            new GridItem() { X = 0, Y = 0 },
            new GridItem() { X = 1, Y = 0 },
            new GridItem() { X = 2, Y = 0 },
            new GridItem() { X = 3, Y = 0 }
        };

        public SDL_Color Color { get; } = new SDL_Color()
        {
            r = 0x00,
            g = 0xFF,
            b = 0xFF,
            a = 0xFF
        };

        public int X { get; set; }
        public int Y { get; set; }



        public void Rotate()
        {
            //The line peace rotates 90 degrees from the center, so we need to find the center,
            //then rotate each point around the center, shifting down


            //Find the center
            int centerX = (GridItems[0].X + GridItems[3].X) / 2;
            int centerY = (GridItems[0].Y + GridItems[3].Y) / 2;

            //Shift the points down
            foreach (var gridItem in GridItems)
            {
                gridItem.X -= centerX;
                gridItem.Y -= centerY;
            }

            //Rotate the points
            foreach (var gridItem in GridItems)
            {
                int oldX = gridItem.X;
                gridItem.X = gridItem.Y;
                gridItem.Y = -oldX;
            }

            //Shift the points back up
            foreach (var gridItem in GridItems)
            {
                gridItem.X += centerX;
                gridItem.Y += centerY;
            }

        }
    }
    public class TetriminoJ : ITetrimino
    {
        public List<GridItem> GridItems { get; } = new()
        {
            new GridItem() { X = 0, Y = 0 },
            new GridItem() { X = 0, Y = 1 },
            new GridItem() { X = 1, Y = 1 },
            new GridItem() { X = 2, Y = 1 }
        };

        public SDL_Color Color { get; } = new SDL_Color()
        {
            r = 0x00,
            g = 0x00,
            b = 0xFF,
            a = 0xFF
        };

        public int X { get; set; }
        public int Y { get; set; }


        public void Rotate()
        {
            //Find the center
            int centerX = (GridItems[0].X + GridItems[3].X) / 2;
            int centerY = (GridItems[0].Y + GridItems[3].Y) / 2;

            //Shift the points down
            foreach (var gridItem in GridItems)
            {
                gridItem.X -= centerX;
                gridItem.Y -= centerY;
            }

            //Rotate the points
            foreach (var gridItem in GridItems)
            {
                int oldX = gridItem.X;
                gridItem.X = gridItem.Y;
                gridItem.Y = -oldX;
            }
        }
    }
    public class TetriminoL : ITetrimino
    {
        public List<GridItem> GridItems { get; } = new()
        {
            new GridItem() { X = 0, Y = 0 },
            new GridItem() { X = 1, Y = 0 },
            new GridItem() { X = 2, Y = 0 },
            new GridItem() { X = 2, Y = 1 }
        };

        public SDL_Color Color { get; } = new SDL_Color()
        {
            r = 0xFF,
            g = 0x7F,
            b = 0x00,
            a = 0xFF
        };

        public int X { get; set; }
        public int Y { get; set; }

        public void Rotate()
        {
            //Find the center
            int centerX = (GridItems[0].X + GridItems[3].X) / 2;
            int centerY = (GridItems[0].Y + GridItems[3].Y) / 2;

            //Shift the points down
            foreach (var gridItem in GridItems)
            {
                gridItem.X -= centerX;
                gridItem.Y -= centerY;
            }

            //Rotate the points
            foreach (var gridItem in GridItems)
            {
                int oldX = gridItem.X;
                gridItem.X = gridItem.Y;
                gridItem.Y = -oldX;
            }
        }
    }
    public class TetriminoO : ITetrimino
    {
        public List<GridItem> GridItems { get; } = new()
        {
            new GridItem() { X = 0, Y = 0 },
            new GridItem() { X = 1, Y = 0 },
            new GridItem() { X = 0, Y = 1 },
            new GridItem() { X = 1, Y = 1 }
        };

        public SDL_Color Color { get; } = new SDL_Color()
        {
            r = 0xFF,
            g = 0xFF,
            b = 0x00,
            a = 0xFF
        };

        public int X { get; set; }
        public int Y { get; set; }

        public void Rotate()
        {
            //Do nothing
        }
    }

    public class TetriminoS : ITetrimino
    {
        public List<GridItem> GridItems { get; } = new()
        {
            new GridItem() { X = 0, Y = 0 },
            new GridItem() { X = 1, Y = 0 },
            new GridItem() { X = 1, Y = 1 },
            new GridItem() { X = 2, Y = 1 }
        };

        public SDL_Color Color { get; } = new SDL_Color()
        {
            r = 0x00,
            g = 0xFF,
            b = 0x00,
            a = 0xFF
        };

        public int X { get; set; }
        public int Y { get; set; }

        public void Rotate()
        {
            //Find the center
            int centerX = (GridItems[0].X + GridItems[3].X) / 2;
            int centerY = (GridItems[0].Y + GridItems[3].Y) / 2;

            //Shift the points down
            foreach (var gridItem in GridItems)
            {
                gridItem.X -= centerX;
                gridItem.Y -= centerY;
            }

            //Rotate the points
            foreach (var gridItem in GridItems)
            {
                int oldX = gridItem.X;
                gridItem.X = gridItem.Y;
                gridItem.Y = -oldX;
            }
        }
    }

    public class TetriminoT : ITetrimino
    {
        public List<GridItem> GridItems { get; } = new()
        {
            new GridItem() { X = 0, Y = 0 },
            new GridItem() { X = 1, Y = 0 },
            new GridItem() { X = 2, Y = 0 },
            new GridItem() { X = 1, Y = 1 }
        };

        public SDL_Color Color { get; } = new SDL_Color()
        {
            r = 0x80,
            g = 0x00,
            b = 0x80,
            a = 0xFF
        };

        public int X { get; set; }
        public int Y { get; set; }

        public void Rotate()
        {
            //Find the center
            int centerX = (GridItems[0].X + GridItems[3].X) / 2;
            int centerY = (GridItems[0].Y + GridItems[3].Y) / 2;

            //Shift the points down
            foreach (var gridItem in GridItems)
            {
                gridItem.X -= centerX;
                gridItem.Y -= centerY;
            }

            //Rotate the points
            foreach (var gridItem in GridItems)
            {
                int oldX = gridItem.X;
                gridItem.X = gridItem.Y;
                gridItem.Y = -oldX;
            }
        }
    }


    public class TetriminoZ : ITetrimino
    {
        public List<GridItem> GridItems { get; } = new()
    {
        new GridItem() { X = 0, Y = 1 },
        new GridItem() { X = 1, Y = 1 },
        new GridItem() { X = 1, Y = 0 },
        new GridItem() { X = 2, Y = 0 }
    };

        public SDL_Color Color { get; } = new SDL_Color()
        {
            r = 0xFF,
            g = 0x00,
            b = 0x00,
            a = 0xFF
        };

        public int X { get; set; }
        public int Y { get; set; }

        public void Rotate()
        {
            //Find the center
            int centerX = (GridItems[0].X + GridItems[3].X) / 2;
            int centerY = (GridItems[0].Y + GridItems[3].Y) / 2;

            //Shift the points down
            foreach (var gridItem in GridItems)
            {
                gridItem.X -= centerX;
                gridItem.Y -= centerY;
            }

            //Rotate the points
            foreach (var gridItem in GridItems)
            {
                int oldX = gridItem.X;
                gridItem.X = gridItem.Y;
                gridItem.Y = -oldX;
            }
        }
    }

    private static Random _random = new Random();

    private enum TetriminoType
    {
        I,
        J,
        L,
        O,
        S,
        T,
        Z
    }


    internal static ITetrimino CreateTetrimino()
    {

        var tetriminoType = (TetriminoType)_random.Next(0, Enum.GetNames(typeof(TetriminoType)).Length);
        ITetrimino tetrimino;
        switch (tetriminoType)
        {
            case TetriminoType.I:
                tetrimino = new TetriminoI();
                break;
            case TetriminoType.J:
                tetrimino = new TetriminoJ();
                break;
            case TetriminoType.L:
                tetrimino = new TetriminoL();
                break;
            case TetriminoType.O:
                tetrimino = new TetriminoO();
                break;
            case TetriminoType.S:
                tetrimino = new TetriminoS();
                break;
            case TetriminoType.T:
                tetrimino = new TetriminoT();
                break;
            case TetriminoType.Z:
                tetrimino = new TetriminoZ();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        tetrimino.X = 3;
        tetrimino.Y = 0;

        return tetrimino;
    }
}