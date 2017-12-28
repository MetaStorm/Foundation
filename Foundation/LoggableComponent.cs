using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;

namespace Foundation {
  public abstract class LoggableComponent<T> : EventLogger<T> where T : LoggableComponent<T>, new() {
  }
}
