using System;
using System.Collections.Generic;
using UnityEngine;

public class BotController : MonoBehaviour {

    [SerializeField] private Game game;
    [SerializeField] private PositionsParent positionParent;
    [SerializeField] private int[] testChipIndexesInGame;

    private HomePositionHandler homePosition;
    private int chipsCount;
    private GameSequence bestMove;
    private GameSequence[] movesMade;
    private Player player;
    private bool tookFromHead;
    private bool movesWereMade;
    private bool botWon = false;
    private int cubeFirst;
    private int cubeLast;
    private int pareIndex = -1;
    private int chipIndexToMove;
    private int realPositionIndex;//need when all chips at home
    private int movesMadeCount;
    private PositionStateStruct[] boardState;
    private List<PositionBase> positions;
    private List<PositionBase> opponentPositions;
    private List<ChipBase> chipList;
    private List<int> chipIndexesInGame;

    private void TestAsignActiveIndexes() {
        testChipIndexesInGame = new int[chipIndexesInGame.Count];
        for (int i = 0; i < chipIndexesInGame.Count; i++) {
            testChipIndexesInGame[i] = chipIndexesInGame[i];
        }
    }


    public void GameStarted(int side) {//0 - white, 1 - black
        chipIndexesInGame = new List<int>();
        positions = new List<PositionBase>();
        if (side == 0) {
            positions = positionParent.GetPositionWhite();
            opponentPositions = positionParent.GetPositionBlack();
        }
        else {
            positions = positionParent.GetPositionBlack();
            opponentPositions = positionParent.GetPositionWhite();
        }
        homePosition = positions[0].GetComponent<HomePositionHandler>();
        player = (Player)side;
    }

    private void AsignBoardState() {
        boardState = new PositionStateStruct[25];
        boardState[0].playerState = Player.Empty;
        for (int i = 1; i < boardState.Length; i++) {
            boardState[i].positionIndex = i;
            boardState[i].playerState = positions[i].player;
            boardState[i].chipsCount = positions[i].GetComponent<PositionHandle>().GetChipListCount();
        }
    }

    public GameSequence[] BotMadeMoves(int cube1, int cube2) {
        int whileTrueHandle = 0;
        movesMadeCount = 0;
        movesWereMade = false;
        tookFromHead = false;
        cubeFirst = cube1;
        cubeLast = cube2;
        if (cubeFirst == cubeLast) {
            pareIndex = 4;
            movesMade = new GameSequence[4];//need to add one by one when move are made, couse there can be situation where is no move
        }
        else {
            movesMade = new GameSequence[2];
        }
        while (!movesWereMade) {
            TurnToMove();
            if (botWon) {
                movesWereMade = true;
                Debug.Log("Bot Won. no more moves");
                game.BotWonGame(player);
                return movesMade;
            }
            whileTrueHandle++;
            if (whileTrueHandle >= 5) {
                movesWereMade = true;
                Debug.Log("ERROR while true!");
            }
        }
        return movesMade;
    }

    public void TurnToMove() {
        AsignBoardState();

        //get cubes from game
        try {
            if (FindBestMove(out int cube)) {
                if (cube != 0)
                    MakeMove(cube, -1);
            }
            else {
                if(realPositionIndex != -1 && chipIndexToMove != -1)
                    MakeMove(cube, realPositionIndex);
            }
        }
        catch (Exception ex) {
            Debug.LogError(ex);
        }
        TestAsignActiveIndexes();
        if (cubeFirst == 0 && cubeLast == 0) {
            movesWereMade = true;
        }
    }

    private bool FindBestMove(out int cube) {
        Debug.Log("Bot have chips in Head - " + (positions[24].player != Player.Empty));
        Debug.Log("tookFromHead - " + tookFromHead);
        if (positions[24].player == player) {//head is not empty
            if (tookFromHead) {
                if (chipIndexesInGame.Count <= 7) {
                    //find farest chip and fast capturing op1-5
                    //just from head and to op1-5
                    cube = FindCubeToMoveFromPos13To23(!(chipIndexesInGame.Count < 5));//if less than 5 - no condition
                    if (cube != -1) return true;
                }
                else if (chipIndexesInGame.Count > 7) {
                    //also not open self 1-6
                    //start block opHome
                    //move to next side
                    //here need to consider prioritizing according to chipCount at each position
                    cube = FindCubeToMoveBlockingOpHome();
                    if (cube != -1) return true;

                    cube = FindCubeToMoveFromPos13To23(true);
                    if (cube != -1) return true;
                }
            }
            else {// not taken from head
                cube = FindCubeToMoveFromHeadToEmpty();
                if (cube != -1) {
                    if (game.GetMovesMade() < 2 && (cubeFirst == cubeLast) && cubeFirst == 6) {
                        tookFromHead = false;
                        pareIndex = 1;
                    }
                    return true;
                }
                cube = FindCubeToMoveFromHeadToSelf();
                if (cube != -1) {
                    if (game.GetMovesMade() < 2 && (cubeFirst == cubeLast) && cubeFirst == 6) {
                        tookFromHead = false;
                        pareIndex = 1;
                    }
                    return true;
                }
            }
        }
        else {//no chips in head
            if (HaveingChipAtPositions(23, 19)) {//need to resort methodes
                //move from op1-5 to home to empty -> to self, if no -> move from opHome to next side but not open pos for op, if no -> move from self 1-5 frwd
                cube = FindCubeToMoveToSelfHomePosEmpty();
                if (cube != -1) return true;
                cube = FindCubeToMoveBlockingOpHome();
                if (cube != -1) return true;
                cube = FindCubeToMoveToSelfHomePosSelf();
                if (cube != -1) return true;
                cube = FindCubeToMoveBlockingOpHome(false);
                if (cube != -1) return true;
                cube = FindCubeToMoveFromOpHomeConditionNotOpenOpHome(true);
                if (cube != -1) return true;
            }
            else if (HaveingChipAtPositions(18, 13)) {
                cube = FindCubeToMoveToSelfHomePosEmpty();
                if (cube != -1) return true;
                //move to home, if no -> from opHome to next side but not last
                //consider chip count in opHome and close/in home to prioritize
                //move all to home fastest way (not op5 to op11 but op5 to op6) from farest pos to home
                int chipCountNextSide = GetChipCountFromListOfPositions(7, 12);
                int chipCountOpHome = GetChipCountFromListOfPositions(13, 18);
                if (chipCountNextSide >= chipCountOpHome) {
                    cube = FindCubeToMoveToSelfHomePosSelf();
                    if (cube != -1) return true;
                }
                else if (chipCountNextSide < chipCountOpHome) {
                    //move from opHome but not open
                    cube = FindCubeToMoveFromOpHomeConditionNotOpenOpHome(true);
                    if (cube != -1) return true;
                }
            }
            else if (HaveingChipAtPositions(12, 7)) {
                //move all chips to home fastest way, if no -> move closer to home, if no move in home considering count of chips in pos
                cube = FindCubeToMoveToSelfHomePosSelfOrEmpty();
                if (cube != -1) return true;
            }
            else if (!HaveingChipAtPositions(24, 7)) {//all in home
                cube = FindCubeToMoveInHome();
                if (cube != -1) return false;
            }
        }
        cube = FindCubeNoConditionToSelf();
        if (cube != -1) return true;
        cube = FindCubeNoConditionToEmpty();
        if (cube != -1) return true;
        //Debug.LogError("bot could not find move!");
        cube = 0;
        return true;
    }

    //bestMove - one from head, one forwrd to capture next side. at start
    //3-4 chips in game - fast capturing opponents 1-5, if no then just take from head ad move frwd
    //less then half of chips in head - starting blocking opponents homePos(all ex 6), but not opening self 1-6 and frwd to next side
    //no chips in head but have in 1-5 pos - cant open opponents home, time to capture home from other side, if not move from self 1-5 to opHome
    //no chip in 1-5 - move to home, if not move from opHome but not last chip
    //consider chip count in opHome and close/in home to prioritize
    //move all to home fastest way (not op5 to op11 but op5 to op6) from farest pos to home
    //same in end game if 5 u move 5 to home and not 6 to 1.
    //in half endGame consider to check chip count (if cubes[2:3] and u have in 2 and 3 chips but one in 5, best to move is 2 and 3)

    //break into small methodes for finding for each move


    private void BubbleSort(int[] array) {
        for (int i = 0; i < array.Length; i++)
            for (int j = 0; j < array.Length - 1; j++)
                if (array[j] > array[j + 1]) {
                    int t = array[j + 1];
                    array[j + 1] = array[j];
                    array[j] = t;
                }
    }

    private List<ChipBase> GetSortedChipListFromFarestPos() {
        List<ChipBase> listToReturn = new List<ChipBase>();
        for (int i = boardState.Length; i > 0; i--) {
            if (boardState[i].playerState == player && boardState[i].chipsCount > 1) {
                listToReturn.AddRange(positions[i].GetComponent<PositionHandle>().GetChipList());
            }
            else if (boardState[i].playerState == player) {
                listToReturn.Add(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
            }
        }
        return listToReturn;
    }

    private List<PositionHandle> BubbleSortPositionsByChipCount(int startIndex, int endIndex) {//last will be with the most count
        List<PositionHandle> listToReturn = new List<PositionHandle>();
        if (startIndex > endIndex) {
            for (int i = startIndex; i > endIndex - 1; i--) {
                PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
                if (currentPosition.player == player)
                    listToReturn.Add(currentPosition);
            }
        }
        else if (endIndex > startIndex) {
            for (int i = startIndex; i < endIndex + 1; i++) {
                PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
                if (currentPosition.player == player)
                    listToReturn.Add(currentPosition);
            }
        }
        for (int i = 0; i < listToReturn.Count; i++) {
            for (int j = 0; j < listToReturn.Count - 1; j++)
                if (listToReturn[j].GetChipListCount() > listToReturn[j + 1].GetChipListCount()) {
                    var position = listToReturn[j];
                    listToReturn.Remove(position);
                    listToReturn.Insert(j + 1, position);
                }
        }
        return listToReturn;//should also take into account the Player State when sorting by the number of chips
    }

    private int GetChipCountFromListOfPositions(List<PositionHandle> listOfPositions) {
        int sumOfChipsCount = 0;
        foreach (var position in listOfPositions) {
            if (position.player == player)
                sumOfChipsCount += position.GetChipListCount();
        }
        return sumOfChipsCount;
    }

    private int GetChipCountFromListOfPositions(int startIndex, int endIndex) {
        int sumOfChipsCount = 0;
        if (startIndex > endIndex) {
            for (int i = startIndex; i >= endIndex; i--) {
                if (boardState[i].playerState == player) {
                    sumOfChipsCount += boardState[i].chipsCount;
                }
            }
        }
        else if (endIndex > startIndex) {
            for (int i = startIndex; i <= endIndex; i++) {
                if (boardState[i].playerState == player) {
                    sumOfChipsCount += boardState[i].chipsCount;
                }
            }
        }
        return sumOfChipsCount;
    }

    private bool HaveingChipAtPositions(int startIndex, int endIndex) {
        if (startIndex > endIndex) {
            for (int i = startIndex; i >= endIndex; i--) {
                if (boardState[i].playerState == player && boardState[i].chipsCount > 0) {
                    return true;
                }
            }
        }
        else if (endIndex > startIndex) {
            for (int i = startIndex; i <= endIndex; i++) {
                if (boardState[i].playerState == player && boardState[i].chipsCount > 0) {
                    return true;
                }
            }
        }
        return false;
    }

    private int FindCubeToMoveToSelfHomePosEmpty() {
        Debug.Log("Inside FindCubeToMoveToSelfHomePosEmpty M");
        for (int i = 12; i > 6; i--) {
            PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
            if (currentPosition.player != player) continue;
            if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
        }
        return -1;
    }

    private int FindCubeToMoveToSelfHomePosSelf() {
        Debug.Log("Inside FindCubeToMoveToSelfHomePosSelf M");
        for (int i = 12; i > 6; i--) {
            PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
            if (currentPosition.player != player) continue;
            if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
        }
        return -1;
    }

    private int FindCubeToMoveToSelfHomePosSelfOrEmpty() {
        Debug.Log("Inside FindCubeToMoveToSelfHomePosSelfOrEmpty M");
        for (int i = 12; i > 6; i--) {
            PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
            if (currentPosition.player != player) continue;
            if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == player || (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i-cubeFirst))) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == player || (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast))) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == player || (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst))) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == player || (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast))) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
        }
        return -1;
    }

    private int FindCubeToMoveFromPos13To23(bool condition) {//condition is not open self 1-6
        Debug.Log("Inside FindCubeToMoveFromPos13To23 M, condition - " + condition);
        //looking from pos13 to 23 to make move in empty or self
        for (int i = 13; i < 24; i++) {
            PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
            if (currentPosition.player != player) continue;
            if (condition && currentPosition.GetChipListCount() == 1 && i >= 19) continue;
            if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast)) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            //to self
            if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
        }
        return -1;
    }

    private int FindCubeToMoveFromOpHomeConditionNotOpenOpHome(bool condition) {//condition is not open op Home
        Debug.Log("Inside FindCubeToMoveFromOpHomeConditionNotOpenOpHome M, condition - " + condition);
        //looking from pos12 to 23 to make move in empty or self
        for (int i = 13; i < 19; i++) {
            PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
            if (currentPosition.player != player) continue;
            if (condition && currentPosition.GetChipListCount() == 1) continue;
            if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {
                if ((positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst)) | positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == Player.Empty | positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {
                if (positions[i - cubeFirst].player == Player.Empty | positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {
                if (positions[i - cubeLast].player == Player.Empty | positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
        }
        return -1;
    }

    private int FindCubeToMoveBlockingOpHome(bool condition = true) {//condition is to not open pos
        Debug.Log("Inside FindCubeToMoveBlockingOpHome M, condition " + condition);
        //looking from pos19 to 23 to make move in empty or self
        for (int i = 23; i >= 19; i--) {
            PositionHandle currentPosition = positions[i].GetComponent<PositionHandle>();
            if (currentPosition.player != player) continue;
            if (condition && currentPosition.GetChipListCount() == 1) continue;
            if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {
                if ((positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst)) | positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {
                if ((positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast)) | positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
            if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {
                if ((positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i-cubeFirst)) | positions[i - cubeFirst].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeFirst;
                }
            }
            else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {
                if ((positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i-cubeLast)) | positions[i - cubeLast].player == player) {
                    chipIndexToMove = chipList.IndexOf(currentPosition.GetChipList()[0]);
                    return cubeLast;
                }
            }
        }
        return -1;
    }

    private int FindCubeToMoveFromHeadToSelf() {
        Debug.Log("Inside FindCubeToMoveFromHeadToSelf M");
        List<ChipBase> chipListAtHead = positions[24].GetComponent<PositionHandle>().GetChipList();
        int chipPositionIndex = positions.IndexOf(chipListAtHead[0].GetCurrentPosition());
        int indexOfChip = chipList.IndexOf(chipListAtHead[0]);
        if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {//cube1 less than cube2
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeFirst, indexOfChip, player)) {
                //need to check barrier rule [blocking 6 pos in a row]
                return cubeFirst;
            }
        }
        else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {//cube2 less than cube1
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeLast, indexOfChip, player)) {
                return cubeLast;
            }
        }
        if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {//cube1 bigger than cube2
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeFirst, indexOfChip, player)) {
                //need to check barrier rule [blocking 6 pos in a row]
                return cubeFirst;
            }
        }
        else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {//cube2 bigger than cube1
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeLast, indexOfChip, player)) {
                return cubeLast;
            }
        }
        return -1;

    }

    private int FindCubeToMoveFromHeadToEmpty() {
        Debug.Log("Inside FindCubeToMoveFromHeadToEmpty M");
        List<ChipBase> chipListAtHead = positions[24].GetComponent<PositionHandle>().GetChipList();
        int chipPositionIndex = positions.IndexOf(chipListAtHead[0].GetCurrentPosition());
        int indexOfChip = chipList.IndexOf(chipListAtHead[0]);
        if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {//cube1 less than cube2
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeFirst, indexOfChip, Player.Empty) && IsSafeBarrierRule(chipPositionIndex, chipPositionIndex - cubeFirst)) {
                return cubeFirst;
            }
        }
        else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {//cube2 less than cube1
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeLast, indexOfChip, Player.Empty) && IsSafeBarrierRule(chipPositionIndex, chipPositionIndex-cubeLast)) {
                return cubeLast;
            }
        }
        if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {//cube1 bigger than cube2
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeFirst, indexOfChip, Player.Empty) && IsSafeBarrierRule(chipPositionIndex, chipPositionIndex-cubeFirst)) {
                return cubeFirst;
            }
        }
        else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {//cube2 bigger than cube1
            if (FindCubeToMoveFromHeadHelper(chipPositionIndex - cubeLast, indexOfChip, Player.Empty) && IsSafeBarrierRule(chipPositionIndex, chipPositionIndex-cubeLast)) {
                return cubeLast;
            }
        }
        return -1;
    }

    private bool FindCubeToMoveFromHeadHelper(int positionIndex, int indexOfChip, Player player) {
        if (positions[positionIndex].player == player) {
            //need to check barrier rule [blocking 6 pos in a row]
            tookFromHead = true;
            if (!chipIndexesInGame.Contains(indexOfChip)) {
                chipIndexesInGame.Add(indexOfChip);
            }
            chipIndexToMove = indexOfChip;
            return true;
        }
        return false;
    }

    private int FindCubeNoConditionToSelf() {
        Debug.Log("Inside FindCubeNoConditionToSelf M");
        if (tookFromHead) {
            for (int i = 0; i < chipIndexesInGame.Count - 1; i++) {
                int indexToMoveFirstCube = positions.IndexOf(chipList[chipIndexesInGame[i]].GetCurrentPosition()) - cubeFirst;
                int indexToMoveSecondCube = positions.IndexOf(chipList[chipIndexesInGame[i]].GetCurrentPosition()) - cubeLast;
                if(indexToMoveFirstCube < 0 || indexToMoveSecondCube <0) { continue; }
                if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {//cube1 less than cube2
                    if (positions[indexToMoveFirstCube].player == player) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {//cube2 less than cube1
                    if (positions[indexToMoveSecondCube].player == player) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeLast;
                    }
                }
                if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {//cube1 bigger than cube2
                    if (positions[indexToMoveFirstCube].player == player) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {//cube2 bigger than cube1
                    if (positions[indexToMoveSecondCube].player == player) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeLast;
                    }
                }
            }
            return -1;
        }
        for (int i = boardState.Length - 1; i > 0; i--) {
            if (boardState[i].playerState == player) {
                if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {//cube1 less than cube2
                    if ((i - cubeFirst) <= 0) continue;
                    if (positions[i - cubeFirst].player == player) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {//cube2 less than cube1
                    if ((i - cubeLast) <= 0) continue;
                    if (positions[i - cubeLast].player == player) {
                        //chip in homeposition and cube is bigger and other chis not in home just skip
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeLast;
                    }
                }
                if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {//cube1 bigger than cube2
                    if ((i - cubeFirst) <= 0) continue;
                    if (positions[i - cubeFirst].player == player) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {//cube2 bigger than cube1
                    if ((i - cubeLast) <= 0) continue;
                    if (positions[i - cubeLast].player == player) {
                        //chip in homeposition and cube is bigger and other chis not in home just skip
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeLast;
                    }
                }
            }
        }
        return -1;
    }
    private int FindCubeNoConditionToEmpty() {
        Debug.Log("Indside FindCubeNoConditionToEmpty M");
        if (tookFromHead) {
            for (int i = 0; i < chipIndexesInGame.Count - 1; i++) {
                int indexToMoveFirstCube = positions.IndexOf(chipList[chipIndexesInGame[i]].GetCurrentPosition()) - cubeFirst;
                int indexToMoveSecondCube = positions.IndexOf(chipList[chipIndexesInGame[i]].GetCurrentPosition()) - cubeLast;
                if (indexToMoveFirstCube < 0 || indexToMoveSecondCube < 0) { continue; }
                if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {//cube1 less than cube2
                    if (positions[indexToMoveFirstCube].player == Player.Empty && IsSafeBarrierRule(i, indexToMoveFirstCube)) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {//cube2 less than cube1
                    if (positions[indexToMoveSecondCube].player == Player.Empty && IsSafeBarrierRule(i, indexToMoveSecondCube)) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeLast;
                    }
                }
                if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {//cube1 bigger than cube2
                    if (positions[indexToMoveFirstCube].player == Player.Empty && IsSafeBarrierRule(i, indexToMoveFirstCube)) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {//cube2 bigger than cube1
                    if (positions[indexToMoveSecondCube].player == Player.Empty && IsSafeBarrierRule(i, indexToMoveSecondCube)) {
                        chipIndexToMove = chipIndexesInGame[i];
                        return cubeLast;
                    }
                }
            }
            return -1;
        }
        for (int i = boardState.Length - 1; i > 0; i--) {
            if (boardState[i].playerState == player) {
                if ((cubeFirst != 0 && cubeFirst <= cubeLast) | cubeLast == 0) {//cube1 less than cube2
                    if ((i - cubeFirst) <= 0) continue;
                    if (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i-cubeFirst)) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast < cubeFirst) | cubeFirst == 0) {//cube2 less than cube1
                    if ((i - cubeLast) <= 0) continue;
                    if (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast)) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeLast;
                    }
                }
                if ((cubeFirst != 0 && cubeFirst >= cubeLast) | cubeLast == 0) {//cube1 bigger than cube2
                    if ((i - cubeFirst) <= 0) continue;
                    if (positions[i - cubeFirst].player == Player.Empty && IsSafeBarrierRule(i, i - cubeFirst)) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeFirst;
                    }
                }
                else if ((cubeLast != 0 && cubeLast > cubeFirst) | cubeFirst == 0) {//cube2 bigger than cube1
                    if ((i - cubeLast) <= 0) continue;
                    if (positions[i - cubeLast].player == Player.Empty && IsSafeBarrierRule(i, i - cubeLast)) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        return cubeLast;
                    }
                }
            }
        }
        return -1;
    }

    private int FindCubeToMoveInHome() {
        realPositionIndex = -1;
        chipIndexToMove = -1;
        Debug.Log("Inside FindCubeToMoveInHome M");
        if (boardState[cubeFirst].playerState == player) {
            chipIndexToMove = chipList.IndexOf(positions[cubeFirst].GetComponent<PositionHandle>().GetChipList()[0]);
            realPositionIndex = cubeFirst;
            return cubeFirst;
        }
        else if (boardState[cubeLast].playerState == player) {
            chipIndexToMove = chipList.IndexOf(positions[cubeLast].GetComponent<PositionHandle>().GetChipList()[0]);
            realPositionIndex = cubeLast;
            return cubeLast;
        }
        else if (cubeFirst >= cubeLast) {
            if (HaveingChipsAtBiggerPositionsInHome(cubeFirst)) {
                //have chips at bigger, than need to move normal
                for (int i = 6; i >= cubeFirst; i--) {
                    if (boardState[i].playerState == player && (boardState[i-cubeFirst].playerState == player || boardState[i - cubeFirst].playerState == Player.Empty)) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        realPositionIndex = i;
                        return cubeFirst;
                    }
                }
            }
            else {
                realPositionIndex = GetPositionIndexToMoveWithBiggerCube(cubeFirst);
                if (realPositionIndex == -1) {
                    return -1;
                }
                chipIndexToMove = chipList.IndexOf(positions[realPositionIndex].GetComponent<PositionHandle>().GetChipList()[0]);
                return cubeFirst;
            }
        }
        else if (cubeLast > cubeFirst) {
            if (HaveingChipsAtBiggerPositionsInHome(cubeLast)) {
                //have chips at bigger, than need to move normal
                for (int i = 6; i >= cubeLast; i--) {
                    if (boardState[i].playerState == player && (boardState[i - cubeLast].playerState == player || boardState[i - cubeLast].playerState == Player.Empty)) {
                        chipIndexToMove = chipList.IndexOf(positions[i].GetComponent<PositionHandle>().GetChipList()[0]);
                        realPositionIndex = i;
                        return cubeLast;
                    }
                }
            }
            else {
                realPositionIndex = GetPositionIndexToMoveWithBiggerCube(cubeLast);
                if (realPositionIndex == -1) {
                    return -1;
                }
                chipIndexToMove = chipList.IndexOf(positions[realPositionIndex].GetComponent<PositionHandle>().GetChipList()[0]);
                return cubeLast;
            }
        }
        return -1;
    }

    private int GetPositionIndexToMoveWithBiggerCube(int cube) {
        for (int i = cube - 1; i > 0; i--) {
            if (boardState[i].playerState == player) {
                return i;
            }
        }
        return -1;
    }

    private bool HaveingChipsAtBiggerPositionsInHome(int startIndex) {
        if (startIndex == 6) {
            return false;
        }
        for (int i = startIndex + 1; i <= 6; i++) {
            if (boardState[i].playerState == player) {
                return true;
            }
        }
        return false;
    }

    private bool IsSafeBarrierRule(int from, int to) {//a rule that does not allow closing all positions from which you can enter the "home"
        int barrierSequence = 0;
        bool isSafe = true;

        for (int j = positions.Count - 1; j > 0; j--) {//Count - 1 because home included
            if (positions[j].player == player && j == from && positions[j].GetComponent<PositionHandle>().GetChipListCount() == 1) {//last chip so position will be open
                barrierSequence = 0;
               // Debug.Log("M IsSafeBarrierRule - last chip so position will be open");
                continue;
            }
            if (positions[j].player == player || (positions[j].player == Player.Empty && to == j)) {//check whether it is already occupied or will be occupied
                barrierSequence++;
            }
            else {
                barrierSequence = 0;
            }
            if (barrierSequence >= 6) {
                bool isBarrier = true;
                Debug.Log("Barrier Rule! botC");
                for (int k = opponentPositions.IndexOf(positions[j]); k > 0; k--) {//checking if there is an opponent's chip in front of the barrier
                    if (opponentPositions[k].player == (player == Player.FirstPlayer ? Player.SecondPlayer : Player.FirstPlayer)) {
                        isBarrier = false;
                        Debug.Log($"Found chip after opponentPosition - {opponentPositions[k]}.");
                    }
                }
                if (isBarrier) {
                    isSafe = false;
                    return isSafe;
                }
            }
        }

        return isSafe;
    }

    private void MakeMove(int cube, int positionIndex) {
        movesMade[movesMadeCount].Cube1 = cubeFirst;
        movesMade[movesMadeCount].Cube2 = cubeLast;
        movesMade[movesMadeCount].PlayerMadeMove = player;
        if (positionIndex == -1) {
            movesMade[movesMadeCount].ToPositionIndex = positions.IndexOf(chipList[chipIndexToMove].GetCurrentPosition()) - cube;
            movesMade[movesMadeCount].FromPositionIndex = positions.IndexOf(chipList[chipIndexToMove].GetCurrentPosition());
        }
        else {
            movesMade[movesMadeCount].ToPositionIndex = positionIndex - cube;
            movesMade[movesMadeCount].FromPositionIndex = positionIndex;
        }
        movesMade[movesMadeCount].ChipIndex = chipIndexToMove;
        if (cubeFirst == cubeLast) {
            pareIndex--;
            if (pareIndex == 0) {
                cubeFirst = 0;
                cubeLast = 0;
            }
        }
        else if (cube == cubeFirst) {
            cubeFirst = 0;
        }
        else if (cube == cubeLast) {
            cubeLast = 0;
        }
        try {
            if (positionIndex == -1) {
                var currentPosition = positions[positions.IndexOf(chipList[chipIndexToMove].GetCurrentPosition()) - cube];
               // currentPosition.GetComponent<PositionHandle>().AddChip(chipList[chipIndexToMove]);
                if (currentPosition.TryGetComponent(out PositionHandle position)) {
                    position.AddChip(chipList[chipIndexToMove]);
                }
                else {
                    Debug.LogError($"chipIndexToMove - {chipIndexToMove}, cube - {cube}");
                }
            }
            else {
                if ((positionIndex - cube) < 0) {
                    var currentPosition = positions[0];
                    botWon = currentPosition.GetComponent<HomePositionHandler>().AddChipToHomeWin(chipList[chipIndexToMove]);
                    //tell gameController that bot Wins
                }
                else if ((positionIndex - cube) == 0) {
                    var currentPosition = positions[positionIndex - cube];
                    botWon = currentPosition.GetComponent<HomePositionHandler>().AddChipToHomeWin(chipList[chipIndexToMove]);
                    //tell gameController that bot Wins
                }
                else {
                    var currentPosition = positions[positionIndex - cube];
                    currentPosition.GetComponent<PositionHandle>().AddChip(chipList[chipIndexToMove]);
                }
            }
        }
        catch (Exception ex) {
            Debug.LogError(ex);
        }
        movesMadeCount++;
    }

    public void SetChips(List<ChipBase> chips) {
        chipList = new List<ChipBase>();
        chipList = chips;
    }

    public Player GetPlayerState() {
        return player;
    }
}
