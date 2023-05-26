using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortBuilder
{
    internal class Port
    {
        public int landID = 0;
        public int seaID = 0;
        public (float x, float y) position = (0, 0);
        public (float c, float s) rotation = (0, 0);
        public float scale = 1;

        public Port(int landID, int seaID, float x, float y) {
            this.landID = landID;
            this.seaID = seaID;
            this.position = (x, y);
        }
        public Port() { }

        //tostring
        public override string ToString() {
            return "LandID: " + landID + " SeaID: " + seaID + " Position: " + position + " Rotation: " + rotation;
        }

        public string WriteLocator() {
            //round roatation values to 6 decimal places
            rotation.c = (float)Math.Round(rotation.c, 6);
            rotation.s = (float)Math.Round(rotation.s, 6);

            return "\t\t{\n\t\t\tid=" + landID + "\n\t\t\tposition={ " + position.x + " 0.000000 " + position.y + " }\n\t\t\trotation={ 0.000000 " + rotation.c + " 0.000000 " + rotation.s + " }\n\t\t\tscale={ " + scale + " " + scale + " " + scale + " }\n\t\t}\n";
        }
        public string WritePort() {
            return landID + ";" + seaID + ";" + position.x + ";" + position.y+"\n";
        }
        
    }
}
