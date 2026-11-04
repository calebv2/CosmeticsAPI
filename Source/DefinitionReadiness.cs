using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace ATT.Cosmetics
{
 internal sealed class DefinitionReadiness<T> where T:class
 {
  sealed class State {internal T Value;internal Exception Error;internal TaskCompletionSource<T> Waiting;}
  readonly Dictionary<uint,State> entries=new Dictionary<uint,State>();
  internal void Declare(uint id){entries.Add(id,new State());}
  internal bool TryGet(uint id,out Task<T> task)
  {
   State state;if(!entries.TryGetValue(id,out state)){task=null;return false;}
   if(state.Waiting==null)
   {
    state.Waiting=new TaskCompletionSource<T>();
    if(state.Value!=null)state.Waiting.TrySetResult(state.Value);
    else if(state.Error!=null)state.Waiting.TrySetException(state.Error);
   }
   task=state.Waiting.Task;return true;
  }
  internal void Complete(uint id,T value)
  {
   if(value==null)throw new ArgumentNullException(nameof(value));
   var state=entries[id];if(state.Error!=null||state.Value!=null)throw new InvalidOperationException("Cosmetic definition already resolved");
   state.Value=value;state.Waiting?.TrySetResult(value);
  }
  internal void Fail(uint id,Exception error)
  {
   var state=entries[id];if(state.Value!=null)return;
   state.Error=error??new InvalidOperationException("Cosmetic registration failed");state.Waiting?.TrySetException(state.Error);
  }
 }
}
