using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

public enum TicTacToeState { none, cross, circle }

[System.Serializable]
public class WinnerEvent : UnityEvent<int> { }

public class TicTacToeAI : MonoBehaviour {

	int _aiLevel;

	TicTacToeState[, ] boardState;

	[SerializeField]
	private bool _isPlayerTurn;

	[SerializeField]
	private TicTacToeState playerState = TicTacToeState.cross;
	TicTacToeState aiState = TicTacToeState.circle;

	[SerializeField]
	private GameObject _xPrefab;

	[SerializeField]
	private GameObject _oPrefab;

	public UnityEvent onGameStarted;

	private bool gameOver = false;

	// Will hold the current values of the MinMax algorithm
	private int recursionScore;
	private int optimalScoreTileIndex = -1;

	//Call This event with the player number to denote the winner
	public WinnerEvent onPlayerWin = new WinnerEvent ();

	ClickTrigger[, ] _triggers;

	private void Awake () {
		if (onPlayerWin == null) {
			onPlayerWin = new WinnerEvent ();
		}

		boardState = new TicTacToeState[3, 3];
	}

	public void StartAI (int AILevel) {
		_aiLevel = AILevel;
		StartGame ();
	}

	public void RegisterTransform (int myCoordX, int myCoordY, ClickTrigger clickTrigger) {
		_triggers[myCoordX, myCoordY] = clickTrigger;
	}

	private void StartGame () {
		_triggers = new ClickTrigger[3, 3];
		onGameStarted.Invoke ();
	}

	public void PlayerSelects (int coordX, int coordY) {

		if (_isPlayerTurn && !gameOver) {
			boardState[coordX, coordY] = playerState;
			Debug.Log ("Player win: " + CheckForWin (playerState));
			SetVisual (coordX, coordY, playerState);

			// Change turn to AI

			_isPlayerTurn = false;
			StartCoroutine (AiTurnCoroutine ());
		}
	}

	public void AiSelects (int coordX, int coordY) {

		if (!_isPlayerTurn && !gameOver) {
			Debug.Log ("Selected coordinates: " + coordX + " - " + coordY);
			boardState[coordX, coordY] = aiState;
			Debug.Log ("AI win: " + CheckForWin (aiState));
			SetVisual (coordX, coordY, aiState);

			// Change turn to Player

			_isPlayerTurn = true;
		}
	}

	public void AlgorithmSelects (int coordX, int coordY, TicTacToeState state) {
		boardState[coordX, coordY] = state;
	}

	public void AlgorithmUndo (int coordX, int coordY) {
		boardState[coordX, coordY] = TicTacToeState.none;
	}

	private void SetVisual (int coordX, int coordY, TicTacToeState targetState) {
		Debug.Log ("Available tiles: " + NumberOfAvailableTiles ());

		Instantiate (
			targetState == TicTacToeState.circle ? _oPrefab : _xPrefab,
			_triggers[coordX, coordY].transform.position,
			Quaternion.identity
		);

		CheckForTie ();
	}

	private void CheckForTie () {
		if (!CheckForWin (playerState) && !CheckForWin (aiState) && NumberOfAvailableTiles () == 0) {
			// There is a tie!
			onPlayerWin.Invoke (-1);
			gameOver = true;
		}
	}

	private bool CheckForWin (TicTacToeState state, bool algorithmInProgress = false) {
		// Horizontal

		bool returnValue = false;
		if ((boardState[0, 0] == state &&
				boardState[1, 0] == state &&
				boardState[2, 0] == state
			) || (
				boardState[0, 1] == state &&
				boardState[1, 1] == state &&
				boardState[2, 1] == state
			) || (
				boardState[0, 2] == state &&
				boardState[1, 2] == state &&
				boardState[2, 2] == state)) {
			returnValue = true;
		}

		// Vertical
		if ((boardState[0, 0] == state &&
				boardState[0, 1] == state &&
				boardState[0, 2] == state
			) || (
				boardState[1, 0] == state &&
				boardState[1, 1] == state &&
				boardState[1, 2] == state
			) || (
				boardState[2, 0] == state &&
				boardState[2, 1] == state &&
				boardState[2, 2] == state
			)) {
			returnValue = true;

		}

		// Diagonal
		if ((boardState[0, 0] == state &&
				boardState[1, 1] == state &&
				boardState[2, 2] == state
			) || (
				boardState[0, 2] == state &&
				boardState[1, 1] == state &&
				boardState[2, 0] == state
			)) {
			returnValue = true;
		}

		if (returnValue && !algorithmInProgress) {
			gameOver = true;
			onPlayerWin.Invoke (state == playerState ? 0 : 1);
		}
		return returnValue;

	}

	private void SelectRandomAIMove (List<Vector2> coordinateList) {
		if (NumberOfAvailableTiles () > 0) {
			Vector2 coordinate = coordinateList[UnityEngine.Random.Range (0, coordinateList.Count)];

			AiSelects ((int) coordinate.x, (int) coordinate.y);
		}
	}

	private List<Vector2> AvailableTileList () {
		List<Vector2> tileList = new List<Vector2> ();

		for (int i = 0; i < 3; i++) {
			for (int j = 0; j < 3; j++) {
				if (boardState[i, j] == TicTacToeState.none) {
					tileList.Add (new Vector2 (i, j));
				}
			}
		}

		return tileList;
	}

	private int NumberOfAvailableTiles () {
		int counter = 0;
		for (int i = 0; i < 3; i++) {
			for (int j = 0; j < 3; j++) {
				if (boardState[i, j] == TicTacToeState.none) {
					counter++;
				}
			}
		}

		return counter;
	}

	private IEnumerator AiTurnCoroutine () {

		yield return null;
		// Call the MinMax algorithm. It will store the (for the player) worst move in optimalScoreButtonIndex.
		// What is worst for the player, is the best for the AI.
		IEnumerator minMaxEnumerator = MinMaxCoroutine (1);
		// Force the coroutine to do everything in one frame
		while (minMaxEnumerator.MoveNext ()) { }

		// Select optimal score.
		// This is where you change the difficulty
		if (_aiLevel == 0 || optimalScoreTileIndex == -1) {
			SelectRandomAIMove (AvailableTileList ());
		} else {
			AiSelects ((int) GetTileCoordinatesFromIndex (optimalScoreTileIndex).x, (int) GetTileCoordinatesFromIndex (optimalScoreTileIndex).y);
		}
	}

	/// <summary>
	/// Min Max algorithm to find the best and worse moves.
	/// This Method stores the current best and worst moves in
	/// highestCurrentScoreIndex and lowestCurrentScoreIndex as a side effect.
	/// </summary>
	/// <param name="depth">Depth - the number of recursion step for weighting the scores</param>
	/// <returns>The sum of scores of all possible steps from the current recursion level downwards (stored in recursionScore)</returns>
	private IEnumerator MinMaxCoroutine (int depth) {

		yield return null;

		if (CheckForFirstMove ()) {
			yield break;
		}

		// We want to store which field gives us the best (player) or the worst (CPU) score
		int currentBestScore = _isPlayerTurn ? Int32.MinValue : Int32.MaxValue;
		int currentOptimalScoreButtonIndex = -1;

		// Find next free field
		int tileIndex = 0;
		while (tileIndex < NumberOfAvailableTiles ()) {
			if (IsTileFree (tileIndex)) {
				Vector2 tile = GetTileCoordinatesFromIndex (tileIndex);
				int currentScore = 0;

				bool endRecursion = false;

				// End iteration and recursion level when we win, because we don't need to go deeper
				AlgorithmSelects ((int) tile.x, (int) tile.y, _isPlayerTurn ? playerState : aiState);
				if (CheckForWin (_isPlayerTurn ? playerState : aiState, true)) {
					currentScore = (_isPlayerTurn ? 1 : -1) * (10 - depth);
					endRecursion = true;
				} else if (NumberOfAvailableTiles () > 0) {
					// If there are tiles left after the Select and Win Check, we can go deeper in the recursion
					_isPlayerTurn = !_isPlayerTurn; // Switch turns - in the next step we want to simulate the other player

					IEnumerator minMaxEnumerator = MinMaxCoroutine (depth + 1);

					while (minMaxEnumerator.MoveNext ()) { }

					currentScore = recursionScore;
					_isPlayerTurn = !_isPlayerTurn; // Switch turns back
				}

				if ((_isPlayerTurn && currentScore > currentBestScore) || (!_isPlayerTurn && currentScore < currentBestScore)) {
					currentBestScore = currentScore;
					currentOptimalScoreButtonIndex = tileIndex;
				}

				// Undo this step and go to the next field
				AlgorithmUndo ((int) tile.x, (int) tile.y);

				if (endRecursion) {
					// No need to check further fields if there already is a win
					break;
				}
			}
			tileIndex++;
			// Stop if we checked all buttons
		}

		recursionScore = currentBestScore;
		optimalScoreTileIndex = currentOptimalScoreButtonIndex;
		Debug.Log ("score: " + recursionScore);
	}

	private bool CheckForFirstMove () {
		if (NumberOfAvailableTiles () == 8) {
			// If player played middle, we want to use any corner
			if (boardState[1, 1] == playerState) {
				RandomCorner ();
			}
			// Otherwise, use the middle
			else {
				optimalScoreTileIndex = 4;
			}
			return true;
		}
		return false;
	}

	private Vector2 GetTileCoordinatesFromIndex (int tileIndex) {
		return new Vector2 ((int) (tileIndex / 3), tileIndex % 3);
	}

	private bool IsTileFree (int tileIndex) {
		return boardState[(int) GetTileCoordinatesFromIndex (tileIndex).x, (int) GetTileCoordinatesFromIndex (tileIndex).y] == TicTacToeState.none ? true : false;
	}

	// Use some variety and use random to determine an optimal start field. Index will be 0, 2, 6 or 8
	private int RandomCorner () {
		optimalScoreTileIndex = (int) Mathf.Floor (UnityEngine.Random.Range (0, 4));
		if (optimalScoreTileIndex == 1) {
			optimalScoreTileIndex = 6;
		} else if (optimalScoreTileIndex == 3) {
			optimalScoreTileIndex = 8;
		}
		return optimalScoreTileIndex;
	}
}