﻿using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using TaxiFarePrediction;
using System.Diagnostics;

namespace TaxiFarePrediction
{
    class Program
    {
        static readonly string _trainDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-train.csv");
        static readonly string _testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-test.csv");
        static readonly string _modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "Model.zip");

        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CurrentDirectory);

            MLContext mlContext = new MLContext(seed: 0);

            var model = Train(mlContext, _trainDataPath);

            Evaluate(mlContext,
                     model);

            TestSinglePrediction(mlContext, model);
        }

        public static ITransformer Train(MLContext mlContext, string dataPath)
        {
            IDataView dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(dataPath, hasHeader: true, separatorChar: ',');

            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
                                   .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
                                   .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
                                   .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
                                   .Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
                                   .Append(mlContext.Regression.Trainers.FastTree());

            Console.WriteLine("=============== Create and Train the Model ===============");

            var model = pipeline.Fit(dataView);

            Console.WriteLine("=============== End of training ===============");
            Console.WriteLine();

            return model;
        }

        private static void Evaluate(MLContext mlContext, ITransformer model)
        {
            IDataView dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(_testDataPath, hasHeader: true, separatorChar: ',');

            var prediction = model.Transform(dataView);

            var metrics = mlContext.Regression.Evaluate(prediction, "Label", "Score");

            Console.WriteLine();
            Console.WriteLine($"*************************************************");
            Console.WriteLine($"*       Model quality metrics evaluation         ");
            Console.WriteLine($"*------------------------------------------------");
            // <Snippet18>
            Console.WriteLine($"*       RSquared Score:      {metrics.RSquared:0.##}");
            // </Snippet18>
            // <Snippet19>
            Console.WriteLine($"*       Root Mean Squared Error:      {metrics.RootMeanSquaredError:#.##}");
            // </Snippet19>
            Console.WriteLine($"*************************************************");


            //SaveModelAsFile(mlContext, dataView.Schema, model);

        }

        private static void TestSinglePrediction(MLContext mlContext, ITransformer model)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var predictionFunction = mlContext.Model.CreatePredictionEngine<TaxiTrip, TaxiTripFarePrediction>(model);

            var taxiTripSample = new TaxiTrip()
            {
                VendorId = "VTS",
                RateCode = "1",
                PassengerCount = 1,
                TripTime = 1140,
                TripDistance = 3.75f,
                PaymentType = "CRD",
                FareAmount = 0
            };

            var prediction = predictionFunction.Predict(taxiTripSample);

            Console.WriteLine($"**********************************************************************");
            Console.WriteLine($"Predicted fare: {prediction.FareAmount:0.####}, actual fare: 15.5");
            Console.WriteLine($"**********************************************************************");

            stopWatch.Stop();
            Console.WriteLine($"Time Elapsed: {stopWatch.ElapsedMilliseconds} ms");
        }

        private static void SaveModelAsFile(MLContext mlContext, DataViewSchema trainingDataViewSchema, ITransformer model)
        {
            // <SnippetSaveModel>
            mlContext.Model.Save(model, trainingDataViewSchema, _modelPath);
            // </SnippetSaveModel>

            Console.WriteLine("The model is saved to {0}", _modelPath);

        }
    }
}

