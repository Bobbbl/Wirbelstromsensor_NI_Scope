using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.ModularInstruments.NIScope;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
using NationalInstruments;
using System.Collections.ObjectModel;
using System.Windows;

namespace Wirbelstromsensor_NI_Scope
{
    static class FinderToolbox
    {
        public static int FindHighestValueInCollection(Collection<double> listY, IList<double> listX, double minimum = Double.MinValue, double maximum = Double.MaxValue)
        {
            if (listY == null || listX == null)
            {
                return -1;
            }

            int low = FindIndexOfValue(listX, minimum);
            int high = FindIndexOfValue(listX, maximum);

            double oldvalue = Double.MinValue, aktvalue = Double.MinValue;
            int foundindex = -1;

            if (low == -1)
            {
                low = 1;
            }
            if (high == -1)
            {
                high = listY.Count;
            }

            for (int i = 0; i < high; i++)
            {
                if (listY[i] > oldvalue)
                {
                    oldvalue = listY[i];
                    foundindex = i;
                }
            }

            return foundindex;

            
        }

        public static int FindHighestValueInCollection(ICollection<Point> list, double minimum = Double.MinValue, double maximum = Double.MaxValue)
        {
            if (list == null)
            {
                return -1;
            }

            List<double> listX = PointToList(list, "X");
            List<double> listY = PointToList(list, "Y");

            int low = FindIndexOfValue(listY, minimum);
            int high = FindIndexOfValue(listY, maximum);

            double oldvalue = Double.MinValue;
            int foundindex = -1;

            if (low == -1)
            {
                low = 1;
            }
            if (high == -1)
            {
                high = listY.Count;
            }

            for (int i = 0; i < high; i++)
            {
                if (listY[i] > oldvalue)
                {
                    oldvalue = listY[i];
                    foundindex = i;
                }
            }

            return foundindex;
        }

        public static List<double> PointToList(ICollection<Point> list, string v)
        {
            List<double> r = new List<double>();

            switch (v)
            {
                case "X":
                    foreach (Point p in list)
                    {
                        r.Add(p.X);
                    }
                    break;
                case "Y":
                    foreach (Point p in list)
                    {
                        r.Add(p.Y);
                    }
                    break;

                default:
                    return null;
                    
            }

            return r;
        }

        public static int FindIndexOfValue(IEnumerable<double> list ,double value)
        {

            double minDistance = 0;
            int minIndex = -1, i = 0;


            foreach (var item in list)
            {
                var distance = Math.Abs(value - item);

                if (minIndex == -1 || distance < minDistance)
                {
                    minDistance = distance;
                    minIndex = i;
                }

                i++;
            }

            return minIndex;
        }

        public static List<double> ConvertNICollectionToPointCollection(AnalogWaveformCollection<double> list, int channel = 0)
        {
            List<double> dlist = new List<double>();

            foreach (var item in list[channel].Samples)
            {
                dlist.Add(item.Value);
            }

            return dlist;
        }

    }
}
