using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace SK8Controller
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private InputAction pauseAction;
        [SerializeField] private InputAction nextAction;
        [SerializeField] private InputAction previousAction;
        [SerializeField] private InputAction acceptAction;
        [SerializeField] private InputAction denyAction;

        private TMP_Text text;
        private CanvasGroup group;
        private int index;
        private bool open;
        private bool accept;
        private bool deny;

        private string path;
        private string prepend;

        private Page current;

        private Page options;
        private Page controls;

        private void Awake()
        {
            prepend += "Macrosoft Shingles [Version {Application.unityVersion}]\n";
            prepend += "(c) Macrosoft Corporation. All rights reserved.\n";

            path = Environment.GetEnvironmentVariable("USERPROFILE");
            prepend += $"\n{path}>sk8hAck.exe ";

            text = GetComponentInChildren<TMP_Text>();
            group = GetComponentInChildren<CanvasGroup>();

            controls = new Page
            {
                command = "show-controls",
                prepend =
                    "-----------------------------------------------------------\n" +
                    "----- Controller is HEAVILY RECOMMENDED for this demo -----\n" +
                    "-----------------------------------------------------------\n" +
                    "| Control_Type     | Keyboard         | Controller        |\n" +
                    "| Throttle_Forward | W                | Joy_Right_Trigger |\n" +
                    "| Throttle_Back    | S                | Joy_Right_Bumper  |\n" +
                    "| Steer            | Mouse_Movement_X | Joy_Stick_Left    |\n" +
                    "-----------------------------------------------------------\n",
                options = new List<(string, Action<PauseMenu>)>()
                {
                    ("Back", menu => menu.Show(menu.options))
                }
            };

            options = new Page
            {
                command = "pause",
                prepend = "",
                options = new List<(string, Action<PauseMenu>)>
                {
                    ("Resume", menu => menu.Show(null)),
                    ("Reload_Scene", _ => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)),
                    ("List_Controls", menu => menu.Show(menu.controls)),
                    ("Terminate", _ =>
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }),
                }
            };
            
            Show(null);
        }

        private void OnEnable()
        {
            pauseAction.Enable();
            nextAction.Enable();
            previousAction.Enable();
            acceptAction.Enable();
            denyAction.Enable();

            pauseAction.performed += OnPause;
            nextAction.performed += OnNext;
            previousAction.performed += OnPrevious;
            acceptAction.performed += OnAccept;
            denyAction.performed += OnDeny;
        }

        private void OnDisable()
        {
            pauseAction.Enable();
            nextAction.Enable();
            previousAction.Enable();
            acceptAction.Enable();
            denyAction.Enable();

            pauseAction.performed -= OnPause;
            nextAction.performed -= OnNext;
            previousAction.performed -= OnPrevious;
            acceptAction.performed -= OnAccept;
            denyAction.performed -= OnDeny;
        }

        private void OnPause(InputAction.CallbackContext ctx) => Show(open ? null : options);

        private void OnNext(InputAction.CallbackContext ctx)
        {
            index++;
            Show(current);
        }

        private void OnPrevious(InputAction.CallbackContext ctx)
        {
            index--;
            Show(current);
        }

        private void OnAccept(InputAction.CallbackContext ctx)
        {
            accept = true;
            Show(current);
            accept = false;
        }

        private void OnDeny(InputAction.CallbackContext ctx)
        {
            deny = true;
            Show(current);
            deny = false;
        }

        private void Show(Page page)
        {
            current = page;
            open = page != null;

            group.alpha = open ? 1 : 0;
            group.interactable = open;
            group.blocksRaycasts = open;

            if (page == null) return;

            var sb = new StringBuilder(prepend);
            sb.AppendLine(page.command);
            sb.AppendLine(page.prepend);

            var c = page.options.Count;
            index = (index % c + c) % c;
            for (var i = 0; i < page.options.Count; i++)
            {
                var option = page.options[i];
                sb.Append($"{(i == index ? ">" : "")}  {option.Item1}\n");
            }

            text.text = sb.ToString();

            if (accept)
            {
                accept = false;
                page.options[index].Item2(this);
            }
        }

        public class Page
        {
            public string command;
            public string prepend;
            public List<(string, Action<PauseMenu>)> options;
        }
    }
}