
namespace Sorth.Interpreter.Runtime.DataStructures
{

    public interface ContextualData
    {
        void MarkContext();
        void ReleaseContext();
    }

}
