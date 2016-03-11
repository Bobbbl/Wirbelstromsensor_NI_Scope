using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wirbelstromsensor_NI_Scope
{
    interface IPushNotificableList<T> 
    {
        /// <summary>
        /// Fügt den aktuellen wert vorne an die Liste an und schiebt
        /// den Rest nach hinten.
        /// </summary>
        void Push(T arg);
        


    }
}
