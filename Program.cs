using PortBuilder;
using System.Drawing;

internal class Program
{

    private static void Main(string[] args) {
        string localDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        //setting scale to 0 will prevent that group from being made
        float seaPortScale = 1.0f;
        float riverPortScale = 0.66f;
        float rotationOffset = 90f;


        Dictionary<Color, Province> provDict = ParseDefinitions();
        
        ParseDefaultMap(provDict);

        //open province.png
        Bitmap bmp = new(localDir + @"\_Input\provinces.png");

        //create _Output folder if it doesn't exist
        Directory.CreateDirectory(localDir + @"\_Output");

        ParseCities();
        ParseMap(provDict, bmp);
        ParsePorts(provDict);
        FindCostal(provDict, bmp.Width, bmp.Height);


        Dictionary<Color, Province> ParseDefinitions() {
            Console.WriteLine("Parsing definitions...");
            Dictionary<Color, Province> provDict = new();

            string[] lines = File.ReadAllLines(localDir + @"\_Input\definition.csv");
            foreach (string line in lines) {
                string l1 = line.Trim();
                if (l1.Length == 0 || l1.StartsWith("#")) continue;



                //split the line on ; the first part is the id and the next 3 are the rgb values
                string[] parts = l1.Split(';');
                //try parse the id
                if (int.TryParse(parts[0], out int id)) {
                    if (id == 0) continue; //games do not use id 0
                    int r = int.Parse(parts[1]);
                    int g = int.Parse(parts[2]);
                    int b = int.Parse(parts[3]);
                    string name = parts[4];

                    //if key does not exist, add it
                    if (!provDict.ContainsKey(Color.FromArgb(r, g, b))) {
                        provDict.Add(Color.FromArgb(r, g, b), new Province(Color.FromArgb(r, g, b), id, name));
                    }
                }



            }

            //print out the number of provinces
            //Console.WriteLine("Found " + provDict.Count + " provinces.");

            return provDict;
        }
        void ParseDefaultMap(Dictionary<Color, Province> provDict) {
            Console.WriteLine("Parsing default.map...");
            //read all string in default map in the Input folder
            string[] lines = File.ReadAllLines(localDir + @"\_Input\default.map");

            //loop through all lines
            foreach (string line in lines) {
                string l1 = CleanLine(line);
                if (l1.Length == 0) continue;

                //if l1 contains RANGE or LIST
                if (l1.ToLower().Contains("range") || l1.ToLower().Contains("list")) {
                    GetRangeList(l1, provDict);
                }

            }


        }
        void GetRangeList(string line, Dictionary<Color, Province> provDict) {
            string type = line.Split("=")[0].Trim().ToLower();

            //if line contains RANGE
            if (line.ToUpper().Contains("RANGE")) {
                //split the line on { and }
                string[] parts = line.Split('{', '}')[1].Split();
                //get the first and last number in parts
                int first = -1;
                int last = -1;
                foreach (string part in parts) {
                    //try parse int
                    if (int.TryParse(part, out int num)) {
                        if (first == -1) first = num;
                        else last = num;
                    }
                }
                //loop through all numbers between first and last
                for (int i = first; i <= last; i++) {
                    //find prov with id i
                    foreach (Province prov in provDict.Values) {
                        if (prov.id == i) {
                            if ((prov.type == "sea_zones" && (type == "wasteland" || type == "impassable_terrain"))
                                || (prov.type == "wasteland" || prov.type == "impassable_terrain") && type == "sea_zones") {
                                prov.type = "impassable_sea";
                            }
                            else {
                                //set type of prov
                                prov.type = type;
                            }
                            //print prov id and type
                            //Console.WriteLine(prov.id + " " + prov.type);
                        }
                    }
                }

            }
            else if (line.ToUpper().Contains("LIST")) {
                //split the line on { and }
                string[] parts = line.Split('{', '}')[1].Split();
                //loop through all parts
                foreach (string part in parts) {
                    //try parse int
                    if (int.TryParse(part, out int num)) {
                        //find prov with id num

                        foreach (Province prov in provDict.Values) {
                            if (prov.id == num) {
                                if (prov.type == "sea_zones" && type == "wasteland") {
                                    prov.type = "impassable_sea";
                                }
                                else {
                                    //set type of prov
                                    prov.type = type;
                                }
                                //print prov id and type
                                //Console.WriteLine(prov.id + " " + prov.type);
                            }
                        }
                    }
                }
            }

        }
        void ParseMap(Dictionary<Color, Province> provDict, Bitmap bmp) {
            Console.WriteLine("Parsing map...");
            
            //loop through all pixels
            for (int x = 0; x < bmp.Width; x++) {
                for (int y = 0; y < bmp.Height; y++) {
                    //get color of pixel
                    Color color = bmp.GetPixel(x, y);
                    //if color is not in provDict, continue
                    if (!provDict.ContainsKey(color)) continue;
                    provDict[color].coords.Add((x, y));

                }

                //print progress every 20% of the way
                if (x % (bmp.Width / 5) == 0) {
                    Console.WriteLine((x / (bmp.Width / 100)) + "%");
                }
            }
            

        }
        void ParsePorts(Dictionary<Color, Province> provDict) {
            //open ports.csv
            string[] lines = File.ReadAllLines(localDir + @"\_Input\ports.csv");
            
            //loop through all lines
            foreach (string line in lines) {
                string l1 = CleanLine(line);
                if (l1.Length == 0) continue;

                //split the line on ;
                string[] parts = l1.Split(';');
                //try parse int
                if (int.TryParse(parts[0], out int id)) {
                    Port port = new(id, int.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                    //find prov with id
                    foreach (Province prov in provDict.Values) {
                        if (prov.id == id) {
                            //add port to prov
                            prov.port = port;
                            break;
                        }
                    }

                }
            }
        }

        void FindCostal(Dictionary<Color, Province> provDict, int height, int width) {
            //creat a array of all cords
            bool[,] seaCords = new bool[height, width];
            bool[,] riverCords = new bool[height, width];
            bool[,] borderingSeaCoords = new bool[height, width];
            bool[,] borderingRiverCoords = new bool[height, width];

            //loop through all provs
            foreach (Province prov in provDict.Values) {
                //if prov is sea
                if (prov.type == "sea_zones") {
                    //loop through all cords
                    foreach ((int x, int y) in prov.coords) {
                        //set seaCords[x,y] to true
                        seaCords[x, y] = true;
                    }
                }
                else if (prov.type == "river_provinces") {
                    //loop through all cords
                    foreach ((int x, int y) in prov.coords) {
                        //set seaCords[x,y] to true
                        riverCords[x, y] = true;
                    }
                }
            }

            //loop through all seaCords and riverCords
            for (int x = 0; x < seaCords.GetLength(0); x++) {
                for (int y = 0; y < seaCords.GetLength(1); y++) {
                    //if seaCords[x,y] is true
                    if (seaCords[x, y]) {
                        //loop though the 4 cardinal points, if it is not true in seaCords set it to true in borderingSeaCoords
                        if (x > 0 && !seaCords[x - 1, y]) borderingSeaCoords[x - 1, y] = true;
                        if (x < seaCords.GetLength(0) - 1 && !seaCords[x + 1, y]) borderingSeaCoords[x + 1, y] = true;
                        if (y > 0 && !seaCords[x, y - 1]) borderingSeaCoords[x, y - 1] = true;
                        if (y < seaCords.GetLength(1) - 1 && !seaCords[x, y + 1]) borderingSeaCoords[x, y + 1] = true;
                    }
                    //if riverCords[x,y] is true
                    else if (riverCords[x, y]) {
                        //loop though the 4 cardinal points, if it is not true in riverCords set it to true in borderingRiverCoords
                        if (x > 0 && !riverCords[x - 1, y]) borderingRiverCoords[x - 1, y] = true;
                        if (x < riverCords.GetLength(0) - 1 && !riverCords[x + 1, y]) borderingRiverCoords[x + 1, y] = true;
                        if (y > 0 && !riverCords[x, y - 1]) borderingRiverCoords[x, y - 1] = true;
                        if (y < riverCords.GetLength(1) - 1 && !riverCords[x, y + 1]) borderingRiverCoords[x, y + 1] = true;
                    }
                }
            }

            //remove any seaCords from borderingRiverCoords and similary for the other way around
            for (int x = 0; x < borderingRiverCoords.GetLength(0); x++) {
                for (int y = 0; y < borderingRiverCoords.GetLength(1); y++) {
                    if (borderingRiverCoords[x, y] && seaCords[x, y]) {
                        borderingRiverCoords[x, y] = false;
                    }
                    if (borderingSeaCoords[x, y] && riverCords[x, y]) {
                        borderingSeaCoords[x, y] = false;
                    }
                }
            }

            //convert borderingSeaCoords and borderingRiverCoords to a hashset of cords
            HashSet<(int x, int y)> borderingSeaCoordsSet = new();
            HashSet<(int x, int y)> borderingRiverCoordsSet = new();

            for (int x = 0; x < borderingSeaCoords.GetLength(0); x++) {
                for (int y = 0; y < borderingSeaCoords.GetLength(1); y++) {
                    if (borderingSeaCoords[x, y]) {
                        borderingSeaCoordsSet.Add((x, y));
                    }
                    if (borderingRiverCoords[x, y]) {
                        borderingRiverCoordsSet.Add((x, y));
                    }
                }
            }

            //loop through all provs
            foreach (Province prov in provDict.Values) {
                //if the intersiction of prov.coords and borderingSeaCoordsSet is not empty set prov.coastal to sea
                if (prov.coords.Intersect(borderingSeaCoordsSet).Count() > 0) {
                    prov.coastal = "sea";
                }
                else if (prov.coords.Intersect(borderingRiverCoordsSet).Count() > 0) {
                    prov.coastal = "river";
                }

            }

            /*
            DebugDrawImage(borderingRiverCoords, "RiverBorder");
            DebugDrawImage(borderingSeaCoords, "SeaBorder");
            */

            List<Province> coastalSeaProvs = FindMissingPorts(provDict, "sea");
            List<Province> coastalRiverProvs = FindMissingPorts(provDict, "river");

            //create port.csv and port_locator.txt in the output folder
            using StreamWriter portWriter = new(localDir + @"\_Output\port.csv");
            using StreamWriter portLocatorWriter = new(localDir + @"\_Output\port_locator_output.txt");


            //write existing ports to port.csv
            portWriter.WriteLine("LandProvince;SeaZone;x;y;");
            foreach (Province prov in provDict.Values) {
                if (prov.port != null) {
                    portWriter.WriteLine(prov.port.ToString());
                }
            }

            if (seaPortScale >= 0) {
                portWriter.WriteLine("#AutoSea");
                foreach (Province prov in coastalSeaProvs) {
                    FindPortPosition(provDict, prov, borderingSeaCoordsSet, "sea_zones", portWriter, portLocatorWriter);
                }
            }
            if (riverPortScale >= 0) {
                portWriter.WriteLine("#AutoRiver");
                foreach (Province prov in coastalRiverProvs) {
                    FindPortPosition(provDict, prov, borderingRiverCoordsSet, "river_provinces", portWriter, portLocatorWriter);
                }
            }

            portWriter.WriteLine("end;end;-1;-1;");


            portWriter.Close();
            portLocatorWriter.Close();
        }

        void DebugDrawImage(bool[,] coorList, string name) {
            //create a new bitmap
            Bitmap bmp = new(coorList.GetLength(0), coorList.GetLength(1));
            //loop through all cords
            for (int x = 0; x < coorList.GetLength(0); x++) {
                for (int y = 0; y < coorList.GetLength(1); y++) {
                    //if coorList[x,y] is true set pixel to black
                    if (coorList[x, y]) {
                        bmp.SetPixel(x, y, Color.Black);
                    }
                }
            }
            //save image
            bmp.Save(localDir + @"\_Output\debug_"+name+".png");
        }

        List<Province> FindMissingPorts(Dictionary<Color, Province> provDict, string coastalType) {
            //if a prov is coastal and has no port, add it to the list
            List<Province> MissingPortPorvs = provDict.Values.Where(x => x.coastal == coastalType && x.port == null && x.type == "").ToList();

            //draw the missing provs to a image
            Bitmap bmp2 = new(bmp.Width, bmp.Height);
            foreach (Province prov in MissingPortPorvs) {
                foreach ((int x, int y) in prov.coords) {
                    bmp2.SetPixel(x, y, prov.color);
                }
            }
            bmp2.Save(localDir + @"\_Output\debug_MissingPorts_"+coastalType+".png");

            return MissingPortPorvs;

        }

        void FindPortPosition(Dictionary<Color, Province> provDict, Province prov, HashSet<(int x, int y)> borderingCoordSet, string type, StreamWriter portWriter, StreamWriter portLocatorWriter) {
            //create a hashset of all coords that are in both prov.coords and borderingCoordSet
            HashSet<(int x, int y)> provCoast = prov.coords.Intersect(borderingCoordSet).ToHashSet();

            //if provCoast is empty return
            if (provCoast.Count == 0) {
                Console.WriteLine("\n\tNo coast found for " + prov.name+"\n");
                return;
            }

            //calculate center of prov
            prov.GetCenter();

            (int x, int y) startPoint = prov.center;
            if (prov.cityCenter != (0, 0)) {
                //average city and center to get a point to start from
                startPoint = ((int)Math.Round((prov.center.x + prov.cityCenter.x) / 2.0), (int)Math.Round((prov.center.y + prov.cityCenter.y) / 2.0));
            }
            //find the coord in the borderingCoordSet that is closest to the cityCenter of the prov, that is also in the prov.coords
            (int x, int y) closestCoord = provCoast.OrderBy(x => Math.Sqrt(Math.Pow(x.x - startPoint.x, 2) + Math.Pow(x.y - startPoint.y, 2))).First();

            prov.port = new Port {
                landID = prov.id,
                position = closestCoord
            };


            //Console.WriteLine("Closest coord: " + closestCoord + " to cityCenter: " + prov.cityCenter + " is contained in prov.coords: " + prov.coords.Contains(closestCoord));
            
            //check the cardinal points around the closestCoord, add any provs that is the same type as the prov to a list of possible port naval ids
            List<int> possiblePortNavalIds = new();
            if (closestCoord.x > 0 && provDict[bmp.GetPixel(closestCoord.x - 1, closestCoord.y)].type == type) {
                possiblePortNavalIds.Add(provDict[bmp.GetPixel(closestCoord.x - 1, closestCoord.y)].id);
            }
            if (closestCoord.x < bmp.Width - 1 && provDict[bmp.GetPixel(closestCoord.x + 1, closestCoord.y)].type == type) {
                possiblePortNavalIds.Add(provDict[bmp.GetPixel(closestCoord.x + 1, closestCoord.y)].id);
            }
            if (closestCoord.y > 0 && provDict[bmp.GetPixel(closestCoord.x, closestCoord.y - 1)].type == type) {
                possiblePortNavalIds.Add(provDict[bmp.GetPixel(closestCoord.x, closestCoord.y - 1)].id);
            }
            if (closestCoord.y < bmp.Width - 1 && provDict[bmp.GetPixel(closestCoord.x, closestCoord.y + 1)].type == type) {
                possiblePortNavalIds.Add(provDict[bmp.GetPixel(closestCoord.x, closestCoord.y + 1)].id);
            }

            Province navalProv = null;
            //if the list has one id that is the most common, set the port to that id
            if (possiblePortNavalIds.Count > 0) {
                //find most common id
                int mostCommonId = possiblePortNavalIds.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key;
                //set port to most common id
                prov.port.seaID = mostCommonId;

                //find the prov with the most common id
                navalProv = provDict.Values.Where(x => x.id == mostCommonId).First();
            }
            else {
                Console.WriteLine("No possible port naval ids found for prov: " + prov.id);
                return;
            }

            Console.WriteLine("provID: " + prov.id + " center: "+ prov.port.position);
            List<float> angles = new();
            for (int i = 2; i < 4; i++) {
                float a = SweepCheck2(i, closestCoord, navalProv);
                if (a != -720) {
                    angles.Add(a);
                }
            }

            /*
                north   0   -0.997653 -0.000000 0.068469
                east    90  -0.769168 -0.000000 -0.639046
                south   180 -0.041528 -0.000000 -0.999137
                west    270  0.658739 -0.000000 -0.752372
            */
            if (angles.Count > 0) {
                
                float angle = angles.Average() + rotationOffset;
                float sin = (float)-Math.Sin(angle * Math.PI / 360);
                float cos = (float)-Math.Cos(angle * Math.PI / 360);
                prov.port.rotation = (cos, sin);
            }
            else {
                Console.WriteLine("\tNo angles found for prov: " + prov.id + " from: " + startPoint);
                prov.port.rotation = (0, 1);
                
            }
            Console.WriteLine(prov.port+"\n");

            if (type.Contains("sea")) prov.port.scale = seaPortScale;
            else prov.port.scale = riverPortScale;

            //vertical correction
            prov.port.position = (prov.port.position.x, bmp.Height - prov.port.position.y);

            portWriter.Write(prov.port.WritePort());
            portLocatorWriter.Write(prov.port.WriteLocator());

        }

        float SweepCheck(int sweepSize, (int, int) centerInt, Province p1, Province p2) {
            //loaer and uper bounds of the sweep
            int lower = (sweepSize / 2) - sweepSize + 1;
            int upper = sweepSize / 2 + 1;

            //Console.WriteLine("Sweeping " + lower + " to " + upper);

            //get the pixles in a sweepSize x sweepSize square around the center as a 2d array
            Color[,] colors = new Color[sweepSize, sweepSize];
            for (int x = lower; x < upper; x++) {
                for (int y = lower; y < upper; y++) {
                    colors[x - lower, y - lower] = bmp.GetPixel(centerInt.Item1 + x, centerInt.Item2 + y);
                }
            }


            bool[] arch = new bool[(2 * sweepSize) - 1];


            for (int x = 0; x < sweepSize; x++) {
                //top
                if (colors[x, 0] == p1.color && colors[sweepSize - x - 1, sweepSize - 1] == p2.color) {
                    arch[x] = true;
                }
                else if (colors[x, 0] == p2.color && colors[sweepSize - x - 1, sweepSize - 1] == p1.color) {
                    arch[x] = true;
                }
                //side
                if (colors[sweepSize - 1, x] == p1.color && colors[sweepSize - x - 1, 0] == p2.color) {
                    arch[sweepSize - 1 + x] = true;
                }
                else if (colors[sweepSize - 1, x] == p2.color && colors[sweepSize - x - 1, 0] == p1.color) {
                    arch[sweepSize - 1 + x] = true;
                }
            }



            //find the largest range of true values in arch and return the rotation
            int largestRange = 0;
            int largestRangeStart = 0;
            int currentRange = 0;
            int currentRangeStart = 0;
            for (int i = 0; i < arch.Length - 1; i++) {
                if (arch[i]) {
                    if (currentRange == 0) {
                        currentRangeStart = i;
                    }
                    currentRange++;
                }
                else {
                    if (currentRange > largestRange) {
                        largestRange = currentRange;
                        largestRangeStart = currentRangeStart;
                    }
                    currentRange = 0;
                }
            }

            //if the largest range is 0 return 0
            if (largestRange == 0) {
                return -720;
            }

            //get the average of the start and end of the largest range
            float angle = (largestRangeStart + largestRangeStart + largestRange) / 2;

            //knowing that 0 in the array is -45 degrees and last is 135 degrees convert the angle to degrees
            angle = (angle + lower) * (180 / (sweepSize - 1));

            return angle;
        }
        
        
        float SweepCheck2(int sweepSize, (int x, int y)centerInt, Province prov) {
            //find all pixles in a round sweep around the center and check if they are the same color as the prov.color and return the average angle of the ones that are
            //sweepSize is the radius of the sweep
            //centerInt is the center of the sweep
            //prov.color is the color to check for

            //returns -720 if no angles are found

            //determin the coordinates that are a radius of sweepSize away from the center
            List<(int x, int y)> coords = new();
            for (int x = -sweepSize; x <= sweepSize; x++) {
                for (int y = -sweepSize; y <= sweepSize; y++) {
                    if (Math.Sqrt(x * x + y * y) <= sweepSize) {
                        coords.Add((x, y));
                    }
                }
            }

            //reorder the coords so the one directly above the center is first and the rest are in a clockwise order
            coords = coords.OrderBy(x => Math.Atan2(x.y, x.x)).ToList();

            //create a new list to store the angles in
            List<float> angles = new();

            //for each coord check if it is the same color as the prov.color and if it is add the angle to the list
            foreach ((int x, int y) coord in coords) {
                if (bmp.GetPixel(centerInt.x + coord.x, centerInt.y + coord.y) == prov.color) {
                    angles.Add((float)(Math.Atan2(coord.y, coord.x)*180/Math.PI));
                }
            }

            //if no angles were found return -720
            if (angles.Count == 0) {
                return -720;
            }

            Console.WriteLine("\tsweepSize: " + sweepSize + " anglesCount: " + angles.Count + " angleValues: " + string.Join(", ", angles) + " angleAV: " + angles.Average());
            

            //return the average of the angles
            return angles.Average();
            
        }
        
        //parse city file
        void ParseCities() {
            //if there is no localDir + @"\_Input\city_locators.txt" file return
            if (!File.Exists(localDir + @"\_Input\city_locators.txt")) {
                Console.WriteLine("No city_locators.txt file found");
                return;
            }

            //read file
            string[] lines = File.ReadAllLines(localDir + @"\_Input\city_locators.txt");

            Dictionary<int, Province> provIDDict = new();
            foreach (Province prov in provDict.Values) {
                provIDDict.Add(prov.id, prov);
            }

            Province currentProv = null;
            //loop through all lines
            foreach (string line in lines) {
                string l1 = CleanLine(line);
                
                if (l1.StartsWith("id")) {
                    //spit on = and get the second part and parse it to int
                    int id = int.Parse(l1.Split('=')[1]);

                    //if the id is in provIDDict set currentProv to the prov with that id
                    if (provIDDict.ContainsKey(id)) {
                        currentProv = provIDDict[id];
                    }
                }

                if (l1.StartsWith("position") && currentProv != null) {
                    List<int> positions = new();

                    //spit on = and get the second part and split it on space and parse each part to int and add it to positions
                    foreach (string pos in l1.Split('=')[1].Split()) {
                        //if it can be cast as an int add it to positions
                        if (float.TryParse(pos, out float result)) {
                            positions.Add((int)result);
                        }
                    }

                    currentProv.cityCenter = (positions[0], bmp.Height - positions[2]);

                }

            }
        }
        

        string CleanLine(string line) {
            return line.Replace("{", " { ").Replace("}", " } ").Replace("=", " = ").Replace("  ", " ").Split('#')[0].Trim();
        }
    }

}