using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YoureRight
{
    // 씬이 바뀌어도 유지되는 게임 전체 관리자 입니다. (DontDestroyOnLoad)
    // 스테이지별 클리어 기록을 모아 두고, 메뉴 → 스테이지 → 결과 순서로 씬을 넘깁니다.
    // 씬에 배치하지 않아도 GameManager.Instance를 처음 호출할 때 자동으로 만들어집니다.
    public class GameManager : SingletonBase<GameManager>
    {
        [Serializable]
        public struct StageRecord
        {
            public string stageName;
            public float time;
        }

        private readonly List<StageRecord> records = new List<StageRecord>();

        public IReadOnlyList<StageRecord> Records => records;

        public float TotalTime
        {
            get
            {
                float total = 0f;
                foreach (StageRecord record in records) total += record.time;
                return total;
            }
        }

        // 메뉴의 Start 버튼: 기록을 비우고 첫 스테이지부터 시작합니다.
        public void StartNewRun()
        {
            records.Clear();
            LoadScene(SceneNames.Stages[0]);
        }

        // StageManager가 스테이지를 클리어하면 호출합니다.
        // 기록을 저장하고 다음 스테이지로, 마지막 스테이지였으면 결과 화면으로 넘어갑니다.
        // addRecord가 false면(튜토리얼) 기록은 남기지 않고 다음 씬으로만 넘어갑니다.
        public void ReportStageClear(string stageName, float time, bool addRecord = true)
        {
            if (addRecord)
            {
                records.Add(new StageRecord { stageName = stageName, time = time });
                Debug.Log($"[GameManager] 기록 저장: {stageName} {TimeFormatter.Format(time)} (누적 {TimeFormatter.Format(TotalTime)})");
            }
            else
            {
                Debug.Log($"[GameManager] 기록 제외: {stageName} {TimeFormatter.Format(time)}");
            }

            LoadScene(GetNextSceneName());
        }

        // 지금 스테이지 다음에 또 스테이지가 있으면 true, 다음이 결과 화면이면 false 입니다. (클리어 창 버튼 글자용)
        public bool HasNextStage => GetNextSceneName() != SceneNames.Result;

        // 현재 씬이 스테이지 목록의 몇 번째인지로 다음 씬을 정합니다.
        // 에디터에서 중간 스테이지 씬을 바로 Play해도 그 다음 스테이지로 이어집니다.
        private string GetNextSceneName()
        {
            int current = Array.IndexOf(SceneNames.Stages, SceneManager.GetActiveScene().name);
            int next = current + 1;

            return current >= 0 && next < SceneNames.Stages.Length ? SceneNames.Stages[next] : SceneNames.Result;
        }

        public void LoadMainMenu()
        {
            LoadScene(SceneNames.Menu);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadScene(string sceneName)
        {
            // 일시정지 상태로 씬을 넘기면 다음 씬도 멈춘 채로 시작하므로 항상 되돌립니다.
            Time.timeScale = 1f;
            Debug.Log($"[GameManager] 씬 이동: {SceneManager.GetActiveScene().name} → {sceneName} (기록 {records.Count}개)");
            SceneManager.LoadScene(sceneName);
        }
    }
}
