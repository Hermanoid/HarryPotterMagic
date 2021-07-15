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
        private Dictionary<OutputPort, int> CurrentPosition;
        private Dictionary<OutputPort, int> StartPosition;
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
            },
            {
                State.Raised, new Dictionary<OutputPort, int>(){
                    { OutputPort.A, 0 },
                    { OutputPort.B, 0 },
                    { OutputPort.C, 750 },
                    { OutputPort.D, 0},
                }
            },
            {
                State.Level_One, new Dictionary<OutputPort, int>(){
                    { OutputPort.A, 0 },
                    { OutputPort.B, 0 },
                    { OutputPort.C, 750 },
                    { OutputPort.D, 1050},
                }
            },
            {
                State.Level_Two, new Dictionary<OutputPort, int>(){
                    { OutputPort.A, 0 },
                    { OutputPort.B, 0 },
                    { OutputPort.C, 750 },
                    { OutputPort.D, 1050 * 2},
                }
            },
            {
                State.Level_Three, new Dictionary<OutputPort, int>(){
                    { OutputPort.A, 0 },
                    { OutputPort.B, 0 },
                    { OutputPort.C, 750 },
                    { OutputPort.D, 1050 * 3},
                }
            }
        };
        private Dictionary<OutputPort, int>[] ObtainitPregrabSequence = new Dictionary<OutputPort, int>[]
        {
            new Dictionary<OutputPort, int>{
                { OutputPort.B, 100 },
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.A, -1500 },
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.C, 2500 },
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.B, 400 },
            },
        };

        private Dictionary<OutputPort, int>[] ObtainitPostgrabSequence = new Dictionary<OutputPort, int>[]
        {
            new Dictionary<OutputPort, int>{
                { OutputPort.C, 600 },
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.A, 0 },
            },
            new Dictionary<OutputPort, int>(){
                { OutputPort.B, 0 },
                { OutputPort.D, 0 },
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.A, -1500 },
                { OutputPort.B, 400 }
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.C, 2500 },
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.B, 900 }
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.A, 0 },
            },
            new Dictionary<OutputPort, int>{
                { OutputPort.C, 600 },
            },
        };
    }
}
