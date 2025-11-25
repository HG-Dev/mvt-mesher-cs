using System.Data;

namespace MvtMesherCore;

public class PbfReadFailure(string message) : DataException(message)
{
}