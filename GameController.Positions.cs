using Lego.Ev3.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public partial class GameController
    {
        private Dictionary<State, Dictionary<OutputPort, int>> PositionDict = new Dictionary<State, Dictionary<OutputPort, int>>()
        {
            {
                State.Startup, new Dictionary<OutputPort, int>(){
                    { OutputPort.A, 0 },
                    { OutputPort.B, 0 },
                    { OutputPort.C, 0 },
                    { OutputPort.D, 0},
                }
            },
            {
                State.Waiting, new Dictionary<OutputPort, int>(){
                    { OutputPort.A, 0 },
                    { OutputPort.B, 2900 },
                    { OutputPort.C, 750 },
                    { OutputPort.D, 0},
                }
            }
        };
    }
}
