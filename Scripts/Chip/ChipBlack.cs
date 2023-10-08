using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChipBlack : ChipBase {

    private void Start() {
        player = Player.SecondPlayer;
        ButtonOnClick();
        canSelect = true;
    }

    private void Update() {
        if (canMove) {
            if (transform.position == target) {
                canMove = false;
            }
            float smoothness = 5f;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * smoothness);
            if (targetRotationX != 0) {
                Quaternion targetRotation = Quaternion.Euler(targetRotationX, 0, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothness);
            }
        }
    }

    private void Game_OnChipButon(object sender, System.EventArgs e) {
        //if (canSellect)
            SelectedChipVisual();
        game.currentChip = this;
    }

    private void SubscribeToOnChipButon() {
        game.OnChipButon += Game_OnChipButon;
    }

    private void UnsubscribeToOnChipButon() {
        game.OnChipButon -= Game_OnChipButon;
    }

    protected void ButtonOnClick() {
        ButtonObject.GetComponent<Button>().onClick.AddListener(() => SubscribeToOnChipButon());
        ButtonObject.GetComponent<Button>().onClick.AddListener(() => game.ButtonChip());
        ButtonObject.GetComponent<Button>().onClick.AddListener(() => UnsubscribeToOnChipButon());
    }

}