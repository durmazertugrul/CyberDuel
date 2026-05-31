namespace CyberDuel.Evaluation
{
    // Tespit performansını ölçmek için Precision, Recall ve F1-Score hesaplar
    // TP, TN, FP, FN sayıları üzerinden confusion matrix de üretir
    public class MetricsReporter
    {
        private int tp = 0;  // Correctly detected attack
        private int tn = 0;  // Correctly ignored normal traffic
        private int fp = 0;  // False alarm (normal flagged as attack)
        private int fn = 0;  // Missed attack

        public void Add(bool predicted, bool actual)
        {
            if (actual == true && predicted == true)
                tp++;
            else if (actual == false && predicted == false)
                tn++;
            else if (actual == false && predicted == true)
                fp++;
            else
                fn++;
        }

        public double GetPrecision()
        {
            if (tp + fp == 0) return 0;
            return (double)tp / (tp + fp);
        }

        public double GetRecall()
        {
            if (tp + fn == 0) return 0;
            return (double)tp / (tp + fn);
        }

        public double GetF1()
        {
            double p = GetPrecision();
            double r = GetRecall();
            if (p + r == 0) return 0;
            return 2 * p * r / (p + r);
        }

        public int Total()
        {
            return tp + tn + fp + fn;
        }

        public void PrintMatrix()
        {
            Console.WriteLine();
            Console.WriteLine("  [ CONFUSION MATRIX ]");
            Console.WriteLine("  TP (Correctly Detected) : " + tp);
            Console.WriteLine("  TN (Correctly Ignored)  : " + tn);
            Console.WriteLine("  FP (False Alarm)        : " + fp);
            Console.WriteLine("  FN (Missed Attack)      : " + fn);
        }

        public void PrintAll()
        {
            PrintMatrix();
            Console.WriteLine();
            Console.WriteLine("  [ IDS PERFORMANCE METRICS ]");
            Console.WriteLine("  Precision  : " + GetPrecision().ToString("F2"));
            Console.WriteLine("  Recall     : " + GetRecall().ToString("F2"));
            Console.WriteLine("  F1-Score   : " + GetF1().ToString("F2"));
            Console.WriteLine("  Total Events: " + Total());
        }

        public void Reset()
        {
            tp = 0;
            tn = 0;
            fp = 0;
            fn = 0;
        }
    }
}