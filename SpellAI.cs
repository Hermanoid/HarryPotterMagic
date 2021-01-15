using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public enum Spells
    {
        Lumos,
        Balloonius_Raisus,
        Shootify,
        Disneyosa,
        Dud
    }
    public class SpellAI
    {
        private const string MODEL_LOCATION = "C:\\Source\\HarryPotterMagic\\SpellNet\\model.onnx";
        //private MLContext mlContext;
        private InferenceSession session;
        public SpellAI()
        {
            session = new InferenceSession(MODEL_LOCATION);
            //mlContext = new MLContext();
            
        }

        public Spells Identify(float[] sample)
        {
            int[] dims = new int[] { 1, sample.Count() };
            var tensor = new DenseTensor<float>(sample, dims);
            var xs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor<float>("dense_input", tensor),
            };

            using (var results = session.Run(xs))
            {
                var one_hot = ((DenseTensor<float>)results.ElementAt(0).Value).Buffer.ToArray();
                return (Spells)Array.IndexOf(one_hot,one_hot.Max());
                // manipulate the results
            }
        }
    }
}
