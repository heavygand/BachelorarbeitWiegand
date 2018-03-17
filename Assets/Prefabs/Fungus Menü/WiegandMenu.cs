using System.Collections;
using System.Collections.Generic;
using Fungus;
using UnityEngine;

public class WiegandMenu :  Menu {

    void Start() {

        SetStandardText("Wie ist das Wetter heute?");

        targetBlock = new Block();

        Say sayCommand = new Say();
        sayCommand.SetStandardText("Es ist wirklich toll!");

        targetBlock.CommandList.Add(sayCommand);
    }
}
