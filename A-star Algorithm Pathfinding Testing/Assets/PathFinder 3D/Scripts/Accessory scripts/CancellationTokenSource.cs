using System.Collections;
using System.Collections.Generic;
using PathFinder3D;

//CancellationTokenSource class implementation
public class CancellationTokenSource{

    public CancellationToken Token;
    public CancellationTokenSource()
    {
        Token = new CancellationToken { IsCancellationRequested = false };
    }
    public void Cancel()
    {
        Token.IsCancellationRequested = true;
    }

}

public class CancellationToken
{
    public bool IsCancellationRequested;
}
