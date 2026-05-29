using OpenTK.Mathematics;
using System.Reflection;
using System.Reflection.Emit;
using Zpg.src.Models;

namespace Zpg.src
{
    public class GameMap
    {
        // Rozměry mapy
        public int width;
        public int height;

        // 2D pole znaků reprezentující mapu
        public char[,] mapMatrix;

        // Výchozí pozice hráče
        public Vector3 startingPosition;

        // Seznam všech úrovní mapy
        public List<MapLevel> mapLevels = new List<MapLevel>();

        // Rozměry jednoho bloku (krychle)
        public float cubeHeight = 3.0f;
        public float cubeDepth = 2.0f;
        public float cubeWidth = 2.0f;

        // Rozměry podlahových dlaždic
        public float floorTileHeight = 0.2f;
        public float floorTileWidth = 2;
        public float floorTileDepth = 2;

        // Výška jedné úrovně včetně podlahy
        public float levelHeight => cubeHeight + floorTileHeight;

        // Počet úrovní mapy
        public int levelCount;

        // Konstruktor - načte mapu ze souboru
        public GameMap(string filename)
        {
            // Načte úrovně z textového souboru
            mapLevels = LoadLevels(".\\src\\maps\\MapWithLevels.txt");

            // Pokud se úrovně načetly, nastaví se hlavní matice mapy
            if (mapLevels.Count > 0)
            {
                width = mapLevels[0].width;
                height = mapLevels[0].height;
                mapMatrix = mapLevels[0].mapMatrix;
            }
            else
            {
                // Pokud se úrovně nenačetly, použije se starý způsob načítání
                LoadMapMatrix(filename);
            }

            levelCount = mapLevels.Count;

            // Získá výchozí pozici hráče
            startingPosition = GetStartingPosition();

            Console.WriteLine($"Loaded {mapLevels.Count} level(s) from {filename}");
            PrintLevels(mapLevels);
        }

        // Načte mapu ze souboru a vrátí seznam úrovní
        public static List<MapLevel> LoadLevels(string filename)
        {
            string mapData = File.ReadAllText(filename);
            return LoadLevelsFromString(mapData);
        }

        // Zpracuje textová data mapy a vrátí seznam úrovní
        public static List<MapLevel> LoadLevelsFromString(string mapData)
        {
            var lines = mapData.Split('\n').Select(line => line.Trim()).ToList();
            var levels = new List<MapLevel>();

            // První řádek obsahuje rozměry a počet úrovní
            string[] dimensions = lines[0].Trim().Split('x');
            if (dimensions.Length < 2)
                throw new Exception("Invalid map format: First line should at least contain widthxheight");

            int width = int.Parse(dimensions[0]);
            int height = int.Parse(dimensions[1]);
            int levelCount = 1;

            if (dimensions.Length >= 3)
                levelCount = int.Parse(dimensions[2]);

            int lineIndex = 1;

            // Vytvoří jednotlivé úrovně mapy
            for (int level = 0; level < levelCount; level++)
            {
                if (lineIndex + height > lines.Count)
                    throw new Exception($"Not enough data for level {level + 1}");

                var mapLevel = new MapLevel(width, height);

                for (int y = 0; y < height; y++)
                {
                    string row = lines[lineIndex + y];
                    if (row.Length < width)
                        throw new Exception($"Line {lineIndex + y + 1} is too short for width {width}");

                    for (int x = 0; x < width; x++)
                    {
                        mapLevel.mapMatrix[x, y] = row[x];
                    }
                }

                levels.Add(mapLevel);
                lineIndex += height;
            }

            return levels;
        }

        // Starší způsob načtení mapové matice ze souboru
        public void LoadMapMatrix(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            string[] first_row = lines[0].Split('x');
            width = int.Parse(first_row[0]);
            height = int.Parse(first_row[1]);
            mapMatrix = new char[width, height];
            lines = lines.Skip(1).ToArray();

            for (int i = 0; i < height; i++)
            {
                string row = lines[i];
                for (int j = 0; j < width; j++)
                {
                    mapMatrix[j, i] = row[j];
                }
            }
        }

        // Zkontroluje návaznost výtahových šachet napříč úrovněmi
        public void CheckElevatorsContinuity(List<MapLevel> levels)
        {
            if (levels == null || levels.Count < 2) return;

            int width = levels[0].width;
            int height = levels[0].height;
            int levelCount = levels.Count;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool[] hasElevator = new bool[levelCount];

                    // Zjistí, kde se nachází výtahy
                    for (int lvl = 0; lvl < levelCount; lvl++)
                    {
                        hasElevator[lvl] = (levels[lvl].mapMatrix[x, y] == 'e');
                    }

                    bool[] isValid = new bool[levelCount];

                    // Označí výtahy, které jsou napojeny na další patro
                    for (int lvl = 0; lvl < levelCount - 1; lvl++)
                    {
                        if (hasElevator[lvl] && hasElevator[lvl + 1])
                        {
                            isValid[lvl] = true;
                            isValid[lvl + 1] = true;
                        }
                    }

                    // Nevalidní výtahy nahradí 'x' a vypíše varování
                    for (int lvl = 0; lvl < levelCount; lvl++)
                    {
                        if (hasElevator[lvl] && !isValid[lvl])
                        {
                            Console.WriteLine($"[WARNING] Elevator at level {lvl} at position ({x}, {y}) was removed – it is not part of a continuous vertical shaft.");
                            levels[lvl].mapMatrix[x, y] = 'x';
                        }
                    }
                }
            }
        }

        // Vrátí novou pozici hráče, pokud vstoupí do výtahu
        public Vector3 GetNextPosition(Vector3 playerPosition, bool up)
        {
            CollisionHandler collisionHandler = new CollisionHandler(new List<Model>(), 0, cubeWidth, cubeHeight, cubeDepth);
            List<(Vector3, int)> elevatorPositions = GetElevatorsPositions();
            float proximityThreshold = 3.0f; // Tolerance vzdálenosti

            foreach (var elevator in elevatorPositions)
            {
                // Přepočet výtahové pozice na střed
                Vector3 elevatorPos = elevator.Item1;
                Vector3 elevatorCenter = new Vector3(
                    elevatorPos.X + cubeWidth / 2,
                    elevatorPos.Y + cubeHeight / 2,
                    elevatorPos.Z + cubeDepth / 2
                );

                Vector2 playerXZ = new Vector2(playerPosition.X, playerPosition.Z);
                Vector2 elevatorXZ = new Vector2(elevatorCenter.X, elevatorCenter.Z);

                float distance = Vector2.Distance(playerXZ, elevatorXZ);
                Console.WriteLine($"Distance to elevator: {distance}");

                // Pokud je hráč blízko výtahu, změní se výšková pozice
                if (distance < proximityThreshold)
                {
                    Vector3 playerHeight = up
                        ? new Vector3(playerPosition.X, playerPosition.Y + levelHeight, playerPosition.Z)
                        : new Vector3(playerPosition.X, playerPosition.Y - levelHeight, playerPosition.Z);

                    return playerHeight;
                }
            }

            return playerPosition; // Jinak vrací původní pozici
        }

        // Získá výchozí pozici hráče ze všech úrovní mapy
        public Vector3 GetStartingPosition()
        {
            Vector3 startingPosition;
            int levelcount = 0;

            foreach (var level in mapLevels)
            {
                for (int i = 0; i < level.height; i++)
                {
                    for (int j = 0; j < level.width; j++)
                    {
                        if (level.mapMatrix[j, i] == '@')
                        {
                            Vector2 startingPositionInMap = new Vector2(j, i);
                            startingPosition.X = startingPositionInMap.X * cubeWidth + 0.5f * cubeWidth;
                            startingPosition.Y = levelcount * levelHeight + 1.7f;
                            startingPosition.Z = startingPositionInMap.Y * cubeDepth + 0.5f * cubeDepth;

                            Console.WriteLine($"Spawn na patře {levelcount}, X = {startingPosition.X}, Z = {startingPosition.Z}, Y = {startingPosition.Y}");
                            return startingPosition;
                        }
                    }
                }
                levelcount++;
            }

            // Pokud se nenašla výchozí pozice, vrátí se pozice (0,0,0)
            return (new Vector3(0, 0, 0));
        }

        // Načte a vygeneruje všechny modely do scény (kostky, výtahy, podlahy)
        public void FillModel(List<Model> models, Shader cubeShader, Material cubeMaterial, Shader elevatorShader, Material elevatorMaterial, Shader floorTileShader, Material floorTileMaterial)
        {
            CheckElevatorsContinuity(mapLevels); // Nejdřív zkontrolujeme výtahy
            FillModelsWithCubes(models, cubeShader, cubeMaterial); // Kostky
            FillModelsWithElevators(models, elevatorShader, elevatorMaterial); // Výtahy
            GenerateFloor(models, floorTileShader, floorTileMaterial); // Podlahy
        }

        // Vygeneruje 3D objekty krychlí podle pozic v mapě
        public void FillModelsWithCubes(List<Model> models, Shader cubeShader, Material cubeMaterial)
        {
            var (cubeVertices, cubeIndices) = SimpleObj.LoadObj(".\\src\\Models\\cube.obj");

            List<(Vector3, int)> cubePositions = GetCubesPositions();
            foreach (var cube in cubePositions)
            {
                var cubeInstance = new Model(cubeVertices, cubeIndices);
                cubeInstance.position = cube.Item1;
                cubeInstance.Shader = cubeShader;
                cubeInstance.Material = cubeMaterial;
                cubeInstance.level = cube.Item2;

                models.Add(cubeInstance);
            }
        }

        // Vygeneruje výtahy jako 3D modely
        public void FillModelsWithElevators(List<Model> models, Shader elevatorShader, Material elevatorMaterial)
        {
            var (elevatorVertices, elevatorIndices) = SimpleObj.LoadObj(".\\src\\Models\\elevator.obj");

            List<(Vector3, int)> elevatorPositions = GetElevatorsPositions();
            foreach (var elevator in elevatorPositions)
            {
                var elevatorInstance = new Model(elevatorVertices, elevatorIndices);
                elevatorInstance.position = elevator.Item1;
                elevatorInstance.Shader = elevatorShader;
                elevatorInstance.Material = elevatorMaterial;
                elevatorInstance.level = elevator.Item2;

                models.Add(elevatorInstance);
            }
        }
        // Vygeneruje podlahové dlaždice pro každou úroveň mapy
        public void GenerateFloor(List<Model> models, Shader tileShader, Material tileMaterial)
        {

            var (tileVertices, tileIndices) = SimpleObj.LoadObj(".\\src\\Models\\floorTile.obj");

            List<(Vector3, int)> tilePositions = GetTilesPositions();
            foreach (var tile in tilePositions)
            {


                var tileInstance = new Model(tileVertices, tileIndices);
                tileInstance.position = tile.Item1;
                tileInstance.Shader = tileShader;
                tileInstance.Material = tileMaterial;
                tileInstance.isFloor = true;
                tileInstance.level = tile.Item2; // Přidání čísla patra
                models.Add(tileInstance);

            }
        }
        // Vrátí pozice všech dlaždic včetně jejich úrovně

        public List<(Vector3, int)> GetTilesPositions()
        {
            List<(Vector3, int)> tilePositions = new List<(Vector3, int)>();
            int levelCount = 0;

            foreach (var level in mapLevels)
            {
                // Výška podlahy pro aktuální patro
                float floorY = levelCount * levelHeight;

                for (int i = 0; i < level.height; i++)
                {
                    for (int j = 0; j < level.width; j++)
                    {
                        // Pokud není na této pozici zeď, výtah nebo prázdno, vytvoříme podlahu
                        if (/*level.mapMatrix[j, i] != 'x' &&*/ level.mapMatrix[j, i] != 'e' && level.mapMatrix[j, i] != '0')
                        {
                            var positionInMap = new Vector2(j, i);
                            var positionInWorld = new Vector3(
                                positionInMap.X * floorTileWidth,
                                floorY,  // Podlaha je přesně na úrovni patra
                                positionInMap.Y * floorTileDepth
                            );
                            tilePositions.Add((positionInWorld, levelCount)); // Přidáme i číslo patra
                        }
                    }
                }
                levelCount++;
            }

            return tilePositions;
        }

        // Vrátí pozice všech krychlí včetně jejich úrovně

        private List<(Vector3, int)> GetCubesPositions()
        {
            List<(Vector3, int)> cubePositions = new List<(Vector3, int)>();

            int levelCount = 0;

            foreach (var level in mapLevels)
            {
                // Výška podlahy pro aktuální patro
                float floorY = levelCount * levelHeight;

                for (int i = 0; i < level.height; i++)
                {
                    for (int j = 0; j < level.width; j++)
                    {
                        if (level.mapMatrix[j, i] == 'x')
                        {
                            var positionInMap = new Vector2(j, i);
                            var positionInWorld = new Vector3(
                                positionInMap.X * cubeWidth,
                                floorY + floorTileHeight, // Bloky stojí na podlaze
                                positionInMap.Y * cubeDepth
                            );
                            cubePositions.Add((positionInWorld, levelCount)); // Přidáme i číslo patra

                        }
                    }
                }
                levelCount++;
            }

            return cubePositions;
        }
        // Vrátí pozice všech výtahů včetně jejich úrovně

        private List<(Vector3, int)> GetElevatorsPositions()
        {
            List<(Vector3, int)> elevatorPositions = new List<(Vector3, int)>();
            int levelCount = 0;

            foreach (var level in mapLevels)
            {
                // Výška podlahy pro aktuální patro
                float floorY = levelCount * levelHeight;

                for (int i = 0; i < level.height; i++)
                {
                    for (int j = 0; j < level.width; j++)
                    {
                        if (level.mapMatrix[j, i] == 'e')
                        {
                            var positionInMap = new Vector2(j, i);
                            var positionInWorld = new Vector3(
                                positionInMap.X * cubeWidth,
                                floorY + floorTileHeight, // Výtahy stojí na podlaze
                                positionInMap.Y * cubeDepth
                            );
                            elevatorPositions.Add((positionInWorld, levelCount)); // Přidáme i číslo patra
                        }
                    }
                }
                levelCount++;
            }

            return elevatorPositions;
        }

        // Pomocná metoda pro výpis všech úrovní mapy do konzole
        public static void PrintLevels(List<MapLevel> levels)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                Console.WriteLine($"Level {i + 1}:");
                PrintLevel(levels[i]);
                Console.WriteLine(); 
            }
        }

        public static void PrintLevel(MapLevel level)
        {
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    Console.Write(level.mapMatrix[x, y]);
                }
                Console.WriteLine();
            }
        }
    }
}