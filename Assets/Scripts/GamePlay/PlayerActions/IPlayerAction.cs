using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerAction
{
    public bool CanExecute();
    public void Execute();
}
