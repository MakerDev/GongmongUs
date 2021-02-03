using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerAction
{
    bool CanExecute();
    void Execute();
}
