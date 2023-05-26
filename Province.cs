using System.Drawing;

namespace PortBuilder
{
    internal class Province
    {
        public int id;
        public string name;
        public Color color;
        public Province newProv = null;
        public HashSet<(int x, int y)> coords = new();
        public string type = "";
        public Port? port = null;
        public string coastal = "";

        public (int x, int y) cityCenter = (0, 0);

        public (int x, int y) center = (0, 0);

        public Province(Color color, int id, string name) {
            this.color = color;
            this.id = id;
            this.name = name;
        }

        public Province() {
        }

        public void GetCenter() {
            int x = 0;
            int y = 0;
            foreach ((int x, int y) coord in coords) {
                x += coord.x;
                y += coord.y;
            }
            x /= coords.Count;
            y /= coords.Count;
            center = (x, y);
        }
        
        

    }
}
