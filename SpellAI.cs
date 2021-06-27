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
    public enum Spell
    {
        Dud = 0,
        Lumos = 1,
        //Balloonius_Raisus,
        Shootify = 2,
        Disneyosa = 3,
        Wingardium_Leviosa = 4,
        Smallo_Munchio = 5,
        Funsizarth = 6,
        Bigcandius = 7,
        Obtainafy = 8,
        Reparo = 9,
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

        public Spell Identify(float[] sample)
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
                return (Spell)Array.IndexOf(one_hot, one_hot.Max());
                // manipulate the results
            }
        }
    }
}
