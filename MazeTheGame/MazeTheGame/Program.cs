namespace MazeTheGame
{
    internal class Program
    {
        enum GamePhases
        {
            Settings = 1,
            TutorialAndBasics,
            MazePhase,
            Fighting,
            EndGame
        }
        static GamePhases gamePhases = GamePhases.Settings;

        static Random random = new Random();
        static bool gameIsRunnig = true;
        static string? userName = "user";
        static bool userInputIsIncorrect = true;

        static (string name, int amountOfProduct, int price)[] storeAssortment = { ("Шашка динамiту", 1, 3), ("Життя", 1, 20) };

        #region Символи
        static char[] mazeWallSkins = { '▒', '░' };
        static char mazeWallSymbol = '▒';

        const char mazeWaySymbol = ' ';
        const char playerSymbol = '☻';
        const char exitSymbol = 'Ω';
        const char chestSymbol = 'x';
        const char chestOpenSymbol = 'o';
        const char enemySymbol = 'Ѫ';
        const char shopSymbol = '₿';
        const char healthBarSymbol = '|';
        const char liveSymbol = '♥';
        //Працюючі символи без єврейських фокусів: ♥ ▓ ▒ ░ ♫ ☼ █ ▲ ☺ ☻
        //Деякі символи: Ѫ Ω ₿ ♥
        #endregion

        static int amountOfChests;
        static int amountOfDynamite = 0;
        static int amountOfCoins = 0;
        static int amountOfEnemies = 0;
        static int amountOfLives = 5;
        static int level = 1;

        #region Шанси дропу та появи
        const int dynamiteDropChance = 40;
        const int coinDropChance = 50;
        static int shopSpawnChance = 20;
        const int chestDropChanceCoefficient = 30;

        const int minLevelToSpawnShop = 5;
        const int minLevelToSpawnEnemy = 5;

        static float chanceToHitSuccessfullyForUser, chanceToHitSuccessfullyForEnemy;
        #endregion

        static int width = 4;
        static int height = 4;

        #region Координати
        static int playerX;
        static int playerY;
        static (int newX, int newY) newPlayerPosition;

        static List<int> enemyX = new List<int>();
        static List<int> enemyY = new List<int>();
        static List<(int newX, int newY)> newEnemyPosition = new List<(int newX, int newY)>();
        #endregion

        #region Фаза файтингу:
        static int gameTurn = 0;
        static int userSkillChoice, enemySkillChoise;
        static int userHeal;

        const int middleHealthValueBorder = 50;
        const int minHealthValueBorder = 20;
        const int healthInNumberToHealthInSymbolsConverter = 5;

        static int userHealthBarInNumbers = 100;
        static int enemyHealthBarInNumbers = 100;
        static int userDamage, enemyDamage;
        static int enemyPoisonedDamage = 0;

        static bool fearEffectIsCastOnUser = false; // Дебаф від ворога

        static (string skillName, int skillChance)[] allAvailableUserSkills = { ("Атака мечем.", 80), ("Атака фаерболом.", 50), ("Моментальне поповнення здоров'я та захист вiд дебафiв.", 70) };
        static (string skillName, int skillChance)[] allAvailableEnemySkills = { ("Атака зубами.", 80), ("Отруєння опонента.", 60), ("Накладання ефекту СТРАХ.", 30), ("Дальня атака.", 50) };

        static List<char> userHealthBarInSymbols = new List<char>();
        static List<char> enemyHealthBarInSymbols = new List<char>();
        #endregion

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; //підтримка символів
            Console.CursorVisible = false;

            do
            {
                switch (gamePhases)
                {
                    case GamePhases.Settings:
                        WriteTextForGamePhase();
                        break;
                    case GamePhases.TutorialAndBasics:
                        WriteTextForGamePhase();
                        break;
                    case GamePhases.MazePhase:
                        WriteTextForGamePhase();
                        WorkOutMazePhase();
                        break;
                    case GamePhases.EndGame:
                    default:
                        WriteTextForGamePhase();
                        break;
                }

                Console.CursorVisible = false;
                gamePhases++;
            } while (gameIsRunnig);
        }
        #region Обробка та створення лабіринту:
        static void DrawMaze(char[,] maze)
        {
            Console.Clear();

            for (int i = 0; i < maze.GetLength(1); i++)
            {
                for (int j = 0; j < maze.GetLength(0); j++)
                {
                    switch (maze[j, i])
                    {
                        case exitSymbol:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case chestOpenSymbol:
                        case chestSymbol:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case shopSymbol:
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            break;
                        default:
                            break;
                    }
                    Console.Write(maze[j, i]);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        static void FillMaze(char[,] maze)
        {
            for (int i = 0; i < maze.GetLength(1); i++)
            {
                for (int j = 0; j < maze.GetLength(0); j++) maze[j, i] = mazeWallSymbol;
            }
        }
        static void CreateMaze(ref char[,] maze)
        {
            FillMaze(maze);

            int startX = 1; int startY = 1;

            maze[startX, startY] = mazeWaySymbol;

            Stack<(int x, int y)> stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));

            do
            {
                var current = stack.Peek();
                List<(int x, int y)> unvisitedNeighbours = GetUnvisitedNeighbors(current, maze);

                if (unvisitedNeighbours.Count > 0)
                {
                    var chosen = unvisitedNeighbours[random.Next(unvisitedNeighbours.Count)];

                    maze[chosen.x, chosen.y] = mazeWaySymbol;
                    maze[(current.x + chosen.x) / 2, (current.y + chosen.y) / 2] = mazeWaySymbol;

                    stack.Push(chosen);
                }
                else stack.Pop();

            } while (stack.Count > 0);



            if (maze.GetLength(0) % 2 == 0)
            {
                ResizeArray(ref maze, maze.GetLength(0) + 1, maze.GetLength(1));
                for (int i = 0; i < maze.GetLength(1); i++) maze[maze.GetLength(0) - 1, i] = mazeWallSymbol;
            }

            if (maze.GetLength(1) % 2 == 0)
            {
                ResizeArray(ref maze, maze.GetLength(0), maze.GetLength(1) + 1);
                for (int i = 0; i < maze.GetLength(0); i++) maze[i, maze.GetLength(1) - 1] = mazeWallSymbol;
            }
        }
        static List<(int x, int y)> GetUnvisitedNeighbors((int x, int y) currentPoint, char[,] maze)
        {
            List<(int x, int y)> unvisitedNeighbours = new List<(int x, int y)>();

            int width = maze.GetLength(0);
            int height = maze.GetLength(1);
            int x = currentPoint.x;
            int y = currentPoint.y;

            if (x - 2 > 0 && maze[x - 2, y] == mazeWallSymbol) unvisitedNeighbours.Add((x - 2, y));
            if (x + 2 < width && maze[x + 2, y] == mazeWallSymbol) unvisitedNeighbours.Add((x + 2, y));
            if (y - 2 > 0 && maze[x, y - 2] == mazeWallSymbol) unvisitedNeighbours.Add((x, y - 2));
            if (y + 2 < height && maze[x, y + 2] == mazeWallSymbol) unvisitedNeighbours.Add((x, y + 2));

            return unvisitedNeighbours;
        }
        static void ResizeArray(ref char[,] array, int newWidth, int newHeight)
        {
            char[,] newArray = new char[newWidth, newHeight];
            width = newWidth;
            height = newHeight;

            for (int i = 0; i < array.GetLength(1); i++)
            {
                for (int j = 0; j < array.GetLength(0); j++) newArray[j, i] = array[j, i];
            }

            array = newArray;
        }
        #endregion

        #region Персонаж, ворог, спавн речей:
        static void PlaceCharacterInRandomPlace(char[,] mazeMap)
        {
            playerX = random.Next(0, mazeMap.GetLength(0) - 1);
            playerY = random.Next(0, mazeMap.GetLength(1) - 1);

            newPlayerPosition = (playerX, playerY);

            if (mazeMap[playerX, playerY] == mazeWaySymbol) return;
            else PlaceCharacterInRandomPlace(mazeMap);
        }
        static void PlaceEnemyInRandomPlace(char[,] mazeMap, int enemyIndex)
        {
            int tempEnemyX = random.Next(0, mazeMap.GetLength(0) - 1);
            int tempEnemyY = random.Next(0, mazeMap.GetLength(1) - 1);

            (int newX, int newY) tempNewEnemyPosition = (tempEnemyX, tempEnemyY);

            bool isEmptySpace = mazeMap[tempEnemyX, tempEnemyY] == mazeWaySymbol;
            bool isEnemyCoordinatesUnequalPlayerCoordinates = playerX != tempEnemyX && playerY != tempEnemyY;

            if (isEmptySpace && isEnemyCoordinatesUnequalPlayerCoordinates)
            {
                enemyX.Add(tempEnemyX);
                enemyY.Add(tempEnemyY);

                newEnemyPosition.Add(tempNewEnemyPosition);
                return;
            }

            else PlaceEnemyInRandomPlace(mazeMap, enemyIndex);
        }
        static void PlaceExit(char[,] mazeMap)
        {
            int exitX = random.Next(0, mazeMap.GetLength(0) - 1);
            int exitY = random.Next(0, mazeMap.GetLength(1) - 1);

            if (mazeMap[exitX, exitY] == mazeWaySymbol) mazeMap[exitX, exitY] = exitSymbol;
            else PlaceExit(mazeMap);
        }
        static void PlaceChests(char[,] mazeMap)
        {
            int chestsX = random.Next(0, mazeMap.GetLength(0) - 1);
            int chestY = random.Next(0, mazeMap.GetLength(1) - 1);

            if (mazeMap[chestsX, chestY] == mazeWaySymbol) mazeMap[chestsX, chestY] = chestSymbol;
            else PlaceChests(mazeMap);
        }
        static void PlaceShop(char[,] mazeMap)
        {
            int shopX = random.Next(0, mazeMap.GetLength(0) - 1);
            int shopY = random.Next(0, mazeMap.GetLength(1) - 1);

            if (mazeMap[shopX, shopY] == mazeWaySymbol) mazeMap[shopX, shopY] = shopSymbol;
            else PlaceShop(mazeMap);
        }
        #endregion

        #region Логіка, керування, скiли:
        static void PlayerControl(char[,] maze)
        {
            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    newPlayerPosition.newY = playerY - 1;
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    newPlayerPosition.newX = playerX - 1;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    newPlayerPosition.newY = playerY + 1;
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    newPlayerPosition.newX = playerX + 1;
                    break;
            }

            CheckNextPlayerPosition(maze);
        }
        static void CheckNextPlayerPosition(char[,] maze)
        {
            int x = newPlayerPosition.newX;
            int y = newPlayerPosition.newY;

            bool insideMap = x >= 0 && x < maze.GetLength(0) && y >= 0 && y < maze.GetLength(1);

            if (insideMap)
            {
                if (maze[x, y] == mazeWallSymbol)
                {
                    TryToDestroyWall(x, y, maze);
                    newPlayerPosition = (playerX, playerY);
                }
                else
                {
                    playerX = x;
                    playerY = y;
                }
            }
            else newPlayerPosition = (playerX, playerY);
        }
        static void CheckCurrentPlayerCoordinate(char[,] maze)
        {
            switch (maze[playerX, playerY])
            {
                case chestSymbol:
                    maze[playerX, playerY] = chestOpenSymbol;

                    if (random.Next(0, 101) <= dynamiteDropChance) amountOfDynamite++;
                    if (random.Next(0, 101) <= coinDropChance) amountOfCoins++;

                    break;
                case exitSymbol:
                    width += random.Next(1, 10);
                    height += random.Next(1, 3);
                    level++;
                    shopSpawnChance += 10;

                    if (level >= minLevelToSpawnEnemy)
                    {
                        amountOfEnemies += random.Next(1, 11);
                    }

                    int chestSpawnFrequency = maze.Length / chestDropChanceCoefficient;
                    amountOfChests = random.Next(0, chestSpawnFrequency);

                    WorkOutMazePhase();
                    break;
                case shopSymbol:
                    WriteColoredText(ConsoleColor.White, $"\nВи потрапили у магазин! \nОберiть iз запропонованого товару щось та купiть це, або залиште магазин!\n", 25);

                    for (int i = 0; i < storeAssortment.Length; i++)
                    {
                        WriteAnimatedText($"{i + 1} - {storeAssortment[i].name}: {storeAssortment[i].amountOfProduct} по {storeAssortment[i].price} монет.");
                    }
                    WriteAnimatedText("Натиснiть клавiшу з цифрою, яка позначена бiля обраного товару!");

                    switch (Console.ReadKey().Key)
                    {
                        default:
                            WriteAnimatedText("До нових зустрiчей!");
                            break;
                        case ConsoleKey.D1:

                            if (amountOfCoins < storeAssortment[0].price)
                            {
                                WriteAnimatedText("У вас недостатньо монет!");
                                goto default;
                            }

                            WriteAnimatedText("\nДякуємо за покупку!");

                            amountOfCoins -= storeAssortment[0].price;
                            amountOfDynamite += storeAssortment[0].amountOfProduct;

                            goto default;
                        case ConsoleKey.D2:

                            if (amountOfCoins < storeAssortment[1].price)
                            {
                                WriteAnimatedText("У вас недостатньо монет!");
                                goto default;
                            }

                            WriteAnimatedText("\nДякуємо за покупку!");

                            amountOfCoins -= storeAssortment[1].price;
                            amountOfLives += storeAssortment[1].amountOfProduct;

                            goto default;
                    }

                    WriteAnimatedText("\nНатиснiть Enter щоб покинути магазин!");
                    Console.ReadLine();
                    break;
                default:
                    break;
            }
        }
        static void TryToDestroyWall(int x, int y, char[,] maze)
        {
            if (amountOfDynamite > 0)
            {
                WriteColoredText(ConsoleColor.Red, "\nНатиснiть пробiл щоб зламати стiну!");
                if (Console.ReadKey().Key is ConsoleKey.Spacebar)
                {
                    amountOfDynamite--;
                    maze[x, y] = mazeWaySymbol;
                }
            }
        }

        static void EnemyControl(char[,] maze, int enemyIndex)
        {
            if (EnemyAndPlayerHaveSimillarCoordinates(enemyIndex))
            {
                TimeToFight(enemyIndex);
                return;
            }

            (int newX, int newY) coordinatesCollection = newEnemyPosition[enemyIndex];

            switch (random.Next(1, 5))
            {
                case 1: //W					
                    coordinatesCollection.newY = enemyY[enemyIndex] - 1;
                    break;
                case 2: //a
                    coordinatesCollection.newX = enemyX[enemyIndex] - 1;
                    break;
                case 3: //s
                    coordinatesCollection.newY = enemyY[enemyIndex] + 1;
                    break;
                case 4: //d
                    coordinatesCollection.newX = enemyX[enemyIndex] + 1;
                    break;
                default:
                    break;
            }

            newEnemyPosition[enemyIndex] = coordinatesCollection;

            CheckNextEnemyPosition(maze, enemyIndex);
        }
        static void CheckNextEnemyPosition(char[,] maze, int enemyIndex)
        {
            int x = newEnemyPosition[enemyIndex].newX;
            int y = newEnemyPosition[enemyIndex].newY;

            bool insideMap = x >= 0 && x < maze.GetLength(0) && y >= 0 && y < maze.GetLength(1);

            if (insideMap)
            {
                if (maze[x, y] == mazeWallSymbol)
                {
                    newEnemyPosition[enemyIndex] = (enemyX[enemyIndex], enemyY[enemyIndex]);
                    EnemyControl(maze, enemyIndex);
                }
                else
                {
                    enemyX[enemyIndex] = x;
                    enemyY[enemyIndex] = y;

                    if (EnemyAndPlayerHaveSimillarCoordinates(enemyIndex)) TimeToFight(enemyIndex);
                }
            }
            else newEnemyPosition[enemyIndex] = (enemyX[enemyIndex], enemyY[enemyIndex]);
        }
        static void TimeToFight(int enemyIndex)
        {
            enemyX.RemoveAt(enemyIndex);
            enemyY.RemoveAt(enemyIndex);
            newEnemyPosition.RemoveAt(enemyIndex);

            amountOfEnemies--;

            gamePhases = GamePhases.Fighting;

            FillEnemyHealthbar();

            FillUserHealthBar();

            WriteTextForGamePhase();
            WorkOutBattlePhase();
        }
        static bool EnemyAndPlayerHaveSimillarCoordinates(int enemyIndex)
        {
            if (enemyX[enemyIndex] == playerX && enemyY[enemyIndex] == playerY) return true;
            return false;
        }
        static void AttackByUser(int skillIndex)
        {
            switch (skillIndex)
            {
                case 1:
                    userDamage = random.Next(10, 15);
                    break;
                case 2:
                    userDamage = random.Next(15, 25);
                    break;
                case 3:
                    enemyPoisonedDamage = 0;
                    fearEffectIsCastOnUser = false;
                    userHeal = random.Next(5, 15);
                    break;
            }

            WriteAnimatedText($"{skillIndex} - {allAvailableUserSkills[skillIndex - 1].skillName}");

            chanceToHitSuccessfullyForUser = allAvailableUserSkills[skillIndex - 1].skillChance;
        }
        static void AttackByEnemy(int skillIndex)
        {
            switch (skillIndex)
            {
                case 1: //"Атака зубами."
                    enemyDamage = random.Next(10, 17);
                    break;
                case 2: //"Отруєння опонента."
                    enemyPoisonedDamage = random.Next(4, 15);
                    break;
                case 3: //"Накладання ефекту СТРАХ."
                    fearEffectIsCastOnUser = true;
                    break;
                case 4: //"Дальня атака."
                    enemyDamage = random.Next(20, 29);
                    break;
            }
        }
        #endregion

        #region Фази Гри:
        #region Фаза Лабирінту:
        static void WorkOutMazePhase()
        {
            char[,] mazeMap = new char[width, height];

            mazeWallSymbol = mazeWallSkins[random.Next(0, mazeWallSkins.Length - 1)];

            CreateMaze(ref mazeMap);
            PlaceExit(mazeMap);

            for (int i = 0; i < amountOfChests; i++) PlaceChests(mazeMap);
            if (level >= minLevelToSpawnShop && random.Next(0, 101) <= shopSpawnChance) PlaceShop(mazeMap);

            PlaceCharacterInRandomPlace(mazeMap);

            for (int i = 0; i < amountOfEnemies; i++) PlaceEnemyInRandomPlace(mazeMap, i);

            while (true)
            {
                DrawMaze(mazeMap);

                for (int i = 0; i < amountOfEnemies; i++) DrawEnemyInMaze(i);

                DrawPlayerInMaze();
                DrawInterface();

                PlayerControl(mazeMap);

                for (int i = 0; i < amountOfEnemies; i++) EnemyControl(mazeMap, i);

                CheckCurrentPlayerCoordinate(mazeMap);
            }
        }
        #endregion

        #region Фаза Файтингу:
        static void WorkOutBattlePhase()
        {
            userHealthBarInNumbers = 100;
            enemyHealthBarInNumbers = 100;

            int typingSpeedInMilliseconds = 5;

            while (userHealthBarInNumbers > 0 && enemyHealthBarInNumbers > 0)
            {
                WriteColoredText(ConsoleColor.Green, "Ваше здоров'я:");

                FillUserHealthBar();
                DrawHP(GetHPColorForUser(), userHealthBarInSymbols);

                Console.SetCursorPosition(userHealthBarInSymbols.Count + 3, Console.GetCursorPosition().Top - 1);
                WriteColoredText(GetHPColorForUser(), $"{userHealthBarInNumbers}%");

                WriteColoredText(ConsoleColor.Red, "\nЗдоров'я противника:");

                FillEnemyHealthbar();
                DrawHP(GetHPColorForEnemy(), enemyHealthBarInSymbols);

                Console.SetCursorPosition(enemyHealthBarInSymbols.Count + 3, Console.GetCursorPosition().Top - 1);
                WriteColoredText(GetHPColorForEnemy(), $"{enemyHealthBarInNumbers}%");

                //стокові настройки
                userDamage = 0;
                enemyDamage = 0;
                userHeal = 0;
                chanceToHitSuccessfullyForUser = 0;
                chanceToHitSuccessfullyForEnemy = 0;

                if (gameTurn % 2 == 0) // хід гравця
                {
                    WriteColoredText(ConsoleColor.White, "\nЕфекти на вас: ", typingSpeedInMilliseconds);

                    if (enemyPoisonedDamage != 0) WriteColoredText(ConsoleColor.DarkGreen, "Отруєння. ", typingSpeedInMilliseconds);

                    if (fearEffectIsCastOnUser) WriteColoredText(ConsoleColor.DarkYellow, "Страх.", typingSpeedInMilliseconds);

                    WriteColoredText(ConsoleColor.Green, "\nВашi скiли:", typingSpeedInMilliseconds);

                    DrawSkills(allAvailableUserSkills, typingSpeedInMilliseconds);

                    do
                    {
                        userSkillChoice = GetInput();
                    } while (!(userSkillChoice > 0 && userSkillChoice <= allAvailableUserSkills.Length));

                    chanceToHitSuccessfullyForUser = allAvailableUserSkills[userSkillChoice - 1].skillChance;

                    if (fearEffectIsCastOnUser)
                    {
                        chanceToHitSuccessfullyForUser -= 40;
                        WriteAnimatedText($"Iз-за ефекту СТРАХ шанс зробити свiй хiд становить: {chanceToHitSuccessfullyForUser}%");
                    }

                    if (chanceToHitSuccessfullyForUser >= random.Next(1, 101))
                    {
                        WriteColoredText(ConsoleColor.Green, "Вам вдасться зробити свiй хiд!", typingSpeedInMilliseconds);

                        AttackByUser(userSkillChoice);

                        if (userHeal != 0)
                        {
                            userHealthBarInNumbers += userHeal;
                            WriteAnimatedText($"Ви поповнили своє здоров'я на {userHeal}!");
                        }
                        else
                        {
                            enemyHealthBarInNumbers -= userDamage;
                            WriteAnimatedText($"Ви нанесете ворогу {userDamage} урону.");
                        }
                    }
                    else WriteColoredText(ConsoleColor.Red, "На жаль вам не вдалося зробити свiй хiд!", typingSpeedInMilliseconds);
                }
                else //Хід противника
                {
                    WriteColoredText(ConsoleColor.Red, "\nСкiли противника:", typingSpeedInMilliseconds);

                    DrawSkills(allAvailableEnemySkills, typingSpeedInMilliseconds);

                    enemySkillChoise = random.Next(1, allAvailableEnemySkills.Length);
                    chanceToHitSuccessfullyForEnemy = allAvailableEnemySkills[enemySkillChoise - 1].skillChance;

                    WriteAnimatedText($"\nВорог обрав: {allAvailableEnemySkills[enemySkillChoise - 1].skillName}");

                    if (chanceToHitSuccessfullyForEnemy >= random.Next(1, 101))
                    {
                        WriteColoredText(ConsoleColor.Red, "Ворогу вдасться зробити свiй хiд!", typingSpeedInMilliseconds);

                        AttackByEnemy(enemySkillChoise);

                        if (enemyPoisonedDamage != 0) WriteAnimatedText($"Ви отримаєте {enemyPoisonedDamage} урону вiд отруєння!");

                        if (fearEffectIsCastOnUser) WriteAnimatedText($"Ворог накладає на вас еффект СТРАХ. Шанс зробити свiй хiд зменшується!");

                        userHealthBarInNumbers -= enemyDamage + enemyPoisonedDamage;
                        WriteAnimatedText($"\nВсього ворог нанесе вам {enemyDamage + enemyPoisonedDamage} урону!");
                    }
                    else WriteColoredText(ConsoleColor.Green, "Ворогу не вдалося зробити свiй хiд", typingSpeedInMilliseconds);
                }

                gameTurn += 1;
                WriteAnimatedText("\nНатиснiть Enter перейти на наступний хiд!");

                Console.ReadLine();
                Console.Clear();
            }

            if (userHealthBarInNumbers > 0 && enemyHealthBarInNumbers <= 0)
            {
                WriteAnimatedText("Ти виграв! Настав час продовжити свою подорож в лабiринтi! " +
                    "\nДоречi з ворога також випало декiлька монет!");
                amountOfCoins += random.Next(5, 10);

                WriteAnimatedText("\nНатиснiть Enter щоб продовжити: ");

                Console.ReadLine();
                Console.Clear();
            }
            else if (amountOfLives > 1)
            {
                WriteAnimatedText("На жаль ти програв. Ти губиш одне своє життя, але продовжуєш свої пригоди!");
                amountOfLives--;

                WriteAnimatedText("\nНатиснiть Enter щоб продовжити: ");

                Console.ReadLine();
                Console.Clear();
            }
            else
            {
                WriteAnimatedText("На жаль, але в тебе не залишилось життiв, тому ти не можеш продовжувати свої пригоди i мусиш залишитись тут НАЗАВЖДИ!");
                WriteAnimatedText("\nНатиснiть Enter щоб продовжити: ");

                Console.ReadLine();
                Console.Clear();

                gamePhases = GamePhases.EndGame;
                Main();
            }
        }
        #endregion

        #region Туторiал до фаз гри

        static void WriteTextForGamePhase()
        {
            Console.Clear();
            string? text = null;

            switch (gamePhases)
            {
                case GamePhases.Settings:
                    do
                    {
                        Console.Clear();
                        WriteAnimatedText("Перед початком гри потрiбно ввести деякi даннi!" +
                            "\nВведiть iм'я для персонажа та натиснiть ENTER: ");
                        userName = Console.ReadLine();
                        userInputIsIncorrect = string.IsNullOrWhiteSpace(userName);
                    } while (userInputIsIncorrect);
                    break;
                case GamePhases.TutorialAndBasics:
                    text = $"Вiтаю {userName} в цiй грi! " +
                $"\nСпочатку тобi варто знати та бути готовим до деяких нюансiв керування в цiй грi. " +
                $"\nПересування персонажем відбувається при натисканнi клавiш: (WASD) або (↑←↓→). " +
                $"\nПри натисканнi клавiши (space) ти використовуєш динамiт(про це далi)." +
                $"\nВсе iнше буде пояснено бiльш детально при проходженнi того чи iншого етапу гри!";
                    WriteAnimatedText(text);

                    WriteAnimatedText("\nНатиснiть Enter щоб продовжити: ");

                    Console.ReadLine();
                    break;
                case GamePhases.MazePhase:
                    text = $"" +
                $"Зараз ти будеш знаходитися в лабиринтi, в якому тобi потрiбно буде дойти до виходу, який має такий символ: ({exitSymbol})" +
                $"\nТакож найчастiше ти будеш зустрiчати ворогiв на своєму шляху(символ ворога: ({enemySymbol})), з якими, на жаль, прийдеться битися, " +
                $"\nале це не так важко як здається на перший погляд!" +
                "" +
                "\nЩе важливим нюансом буде те що лабiринт постiйно розширюється, тому в майбутньому тобi буде все важче " +
                "\nдобиратися до виходу!" +
                "" +
                "\nЩоб цього уникнути, твоїми попередниками на кожному рiвнi були захованi скарби, щоб дiзнатися що в них тобi треба " +
                "\nбуде лише вiдкопати заховане!" +
                "" +
                "\nЦе може бути, як щось позитивне, так i щось негативне, пам'ятай про це!" +
                "\nIз позитивного на данний момент тобi треба знати лише те що там можуть бути монети, якi знадобляться тобi пiзнiше, " +
                "\nа також шашка динамiту, за допомогою якої ти зможеш взривати стiни та робити альтернативний шлях до виходу, " +
                "\nякщо ти розумiєш про що я)." +
                "" +
                "\nЩе в лабiринтi ти зможеш знайти на деяких рiвнях магазини. Саме в них тобi i будуть потрiбнi монети, " +
                "\nза якi ти зможеш купувати життя або додатковий динамiт! Будь обережним i не прогав всi свої життя!" +
                "" +
                "\nI останнє, твiй iнвентар також завжди буде показаний знизу! Удачi вижити!";
                    WriteAnimatedText(text);

                    WriteAnimatedText("\nНатиснiть Enter щоб продовжити: ");

                    Console.ReadLine();
                    break;
                case GamePhases.Fighting:
                    ConsoleKeyInfo userInputKey;

                    do
                    {
                        Console.Clear();
                        WriteAnimatedText("Чи треба пояснити фазу бою?" +
                            "\nНатиснiть (Y) або (N): ");
                        userInputKey = Console.ReadKey();
                    } while (userInputKey.Key != ConsoleKey.Y && userInputKey.Key != ConsoleKey.N);

                    Console.Clear();

                    if (userInputKey.Key == ConsoleKey.N)
                    {
                        text = $"Тодi переходимо до битви!";
                        WriteAnimatedText(text);

                        WriteAnimatedText("\nНатиснiть Enter щоб продовжити: ");

                        Console.ReadLine();
                        break;
                    }

                    text = "Зверху будуть показанi всi твої скiли, якi ти можеш використати. " +
                        "\nТакож бiля кожного скiла завжди буде написано вирогiднiсть того що ти зможеш його використати! " +
                        "\nПриблизно ось так: ";
                    WriteAnimatedText(text);

                    DrawSkills(allAvailableUserSkills);

                    text = "\nТакож ти, як i твiй противник будеш мати полоску здоров'я." +
                        "\nЗвичайно якщо вона дiйде до кiнця то ти або твiй опонент програє." +
                        "\nПриблизно ось так:";
                    WriteAnimatedText(text);

                    WriteColoredText(ConsoleColor.Green, "\nЗдоров`є гравця:", 25);
                    DrawHP(GetHPColorForUser(), userHealthBarInSymbols);

                    WriteColoredText(ConsoleColor.Red, "\nЗдоров`є ворога:", 25);
                    DrawHP(GetHPColorForEnemy(), enemyHealthBarInSymbols);

                    text = "\nТвiй противник також буде мати рiзнi скiли. Вiн буде рандомно обирати їх кожен раз так що будь обережним!!! " +
                        "\nВиглядає наступним чином:";
                    WriteAnimatedText(text);

                    DrawSkills(allAvailableEnemySkills);

                    WriteAnimatedText("\nНатиснiть Enter щоб продовжити: ");

                    Console.ReadLine();
                    break;
                case GamePhases.EndGame:
                    text = $"Дякую шановний/на {userName} що пограли в цю гру, сподiваюся що вам сподобалось i гра була цiкавою! " +
                        $"\nБажаю тобi успiху та до зустрiчi!!!";

                    WriteColoredText(ConsoleColor.Yellow, text, 25);
                    WriteAnimatedText("\nНатиснiть Enter щоб завершити гру: ");

                    gameIsRunnig = false;

                    break;
            }
            Console.Clear();
        }
        #endregion
        #endregion

        #region Рендер та обробка: 
        static void WriteAnimatedText(string text, int delayInMilliseconds = 25)
        {
            for (int i = 0; i < text.Length; i++)
            {
                Console.Write(text[i]);
                Thread.Sleep(delayInMilliseconds);
            }
            Console.WriteLine();
        }
        static void WriteColoredText(ConsoleColor color, string text, int delayInMilliseconds = 0)
        {
            Console.ForegroundColor = color;
            WriteAnimatedText(text, delayInMilliseconds);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        static int GetInput(string message = "Оберiть скiл та впишiть його номер(пiсля чого натиснiть ENTER): ")
        {
            int number = 0;
            bool rightInput = false;

            while (!rightInput)
            {
                WriteAnimatedText(message);
                rightInput = int.TryParse(Console.ReadLine(), out number);
            }

            Console.WriteLine();
            return number;
        }
        static void DrawInterface()
        {
            Console.WriteLine("" +
                $"Кiлькiсть динамiту: {amountOfDynamite}" +
                $"\nКiлькiсть монет: {amountOfCoins}" +
                $"\nРiвень: {level}" +
                $"\nКiлькiсть життiв: ");

            Console.ForegroundColor = ConsoleColor.Green;

            for (int i = 0; i < amountOfLives; i++) Console.Write($"{liveSymbol} ");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }
        static void DrawPlayerInMaze()
        {
            (int x, int y) cursorPreviousPosition = Console.GetCursorPosition();

            Console.ForegroundColor = ConsoleColor.Blue;

            Console.SetCursorPosition(playerX, playerY);
            Console.Write(playerSymbol);
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.SetCursorPosition(cursorPreviousPosition.x, cursorPreviousPosition.y);
        }
        static void DrawEnemyInMaze(int enemyIndex)
        {
            (int x, int y) cursorPreviousPosition = Console.GetCursorPosition();

            Console.ForegroundColor = ConsoleColor.Red;

            Console.SetCursorPosition(enemyX[enemyIndex], enemyY[enemyIndex]);
            Console.Write(enemySymbol);
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.SetCursorPosition(cursorPreviousPosition.x, cursorPreviousPosition.y);
        }
        #endregion

        #region Файтинг
        static void DrawSkills((string name, int chance)[] skills, int delayInMilliseconds = 25)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                WriteAnimatedText($"{i + 1} - {skills[i].name} ({skills[i].chance}%)", delayInMilliseconds);
            }
        }
        static void DrawHP(ConsoleColor color, List<char> healthBar)
        {
            Console.ForegroundColor = color;

            foreach (char symbol in healthBar) Console.Write(symbol);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
        }
        static ConsoleColor GetHPColorForUser()
        {
            ConsoleColor color;

            if (userHealthBarInNumbers > middleHealthValueBorder) color = ConsoleColor.Green;
            else if (userHealthBarInNumbers <= middleHealthValueBorder && userHealthBarInNumbers >= minHealthValueBorder) color = ConsoleColor.Yellow;
            else color = ConsoleColor.Red;

            return color;
        }
        static ConsoleColor GetHPColorForEnemy()
        {
            ConsoleColor color;

            if (enemyHealthBarInNumbers > middleHealthValueBorder) color = ConsoleColor.Green;
            else if (enemyHealthBarInNumbers < middleHealthValueBorder && enemyHealthBarInNumbers > minHealthValueBorder) color = ConsoleColor.Yellow;
            else color = ConsoleColor.Red;

            return color;
        }
        static void FillUserHealthBar()
        {
            while (userHealthBarInSymbols.Count > userHealthBarInNumbers / healthInNumberToHealthInSymbolsConverter)
            {
                userHealthBarInSymbols.RemoveAt(userHealthBarInSymbols.Count - 1);
            }

            while (userHealthBarInSymbols.Count < userHealthBarInNumbers / healthInNumberToHealthInSymbolsConverter)
            {
                userHealthBarInSymbols.Add(healthBarSymbol);
            }

            if (userHealthBarInNumbers < 5) userHealthBarInSymbols.Add(healthBarSymbol);
        }
        static void FillEnemyHealthbar()
        {
            while (enemyHealthBarInSymbols.Count > enemyHealthBarInNumbers / healthInNumberToHealthInSymbolsConverter)
            {
                enemyHealthBarInSymbols.RemoveAt(enemyHealthBarInSymbols.Count - 1);
            }

            while (enemyHealthBarInSymbols.Count < enemyHealthBarInNumbers / healthInNumberToHealthInSymbolsConverter)
            {
                enemyHealthBarInSymbols.Add(healthBarSymbol);
            }

            if (enemyHealthBarInNumbers < 5) enemyHealthBarInSymbols.Add(healthBarSymbol);
        }
        #endregion
    }
}