using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Wirbelstromsensor_NI_Scope
{
    public class ObservablePushCollection<T> : ObservableCollection<T>, IPushNotificableList<T>
    {
        public void Push(T arg)
        {
            this.RemoveAt(this.Count-1);
            this.Add(arg);
        }
    }
}
