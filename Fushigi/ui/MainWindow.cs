﻿using Fushigi.course;
using Fushigi.util;
using Fushigi.windowing;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fushigi.param;
using Fushigi.ui.widgets;
using ImGuiNET;
using System.Numerics;
using System.Diagnostics;
using Silk.NET.SDL;

namespace Fushigi.ui
{
    public class MainWindow
    {

        public MainWindow()
        {
            WindowManager.CreateWindow(out mWindow);   
            mWindow.Load += () => WindowManager.RegisterRenderDelegate(mWindow, Render);
            mWindow.Closing += Close;
            mWindow.Run();
            mWindow.Dispose();
        }

        public void Close()
        {
            UserSettings.Save();
        }

        void LoadFromSettings()
        {
            string romFSPath = UserSettings.GetRomFSPath();
            if (!string.IsNullOrEmpty(romFSPath))
            {
                RomFS.SetRoot(romFSPath);
                misChoosingPreferences = false;
            }

            if (!ParamDB.sIsInit && !string.IsNullOrEmpty(RomFS.GetRoot()))
            {
                ParamDB.Load();
            }

            string? latestCourse = UserSettings.GetLatestCourse();
            if (latestCourse != null)
            {
                mCurrentCourseName = latestCourse;
                mSelectedCourseScene = new(new(mCurrentCourseName));
                mIsChoosingCourse = false;
                misChoosingPreferences = false;
            }
        }

        void DrawMainMenu()
        {
            /* create a new menubar */
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Preferences"))
                    {
                        misChoosingPreferences = true;
                    }

                    if (!string.IsNullOrEmpty(RomFS.GetRoot()) && 
                        !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                    {
                        if (ImGui.MenuItem("Open Course"))
                        {
                            mIsChoosingCourse = true;
                        }
                    }

                    /* Saves the currently loaded course */

                    var text_color = mSelectedCourseScene == null ?
                         ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled] :
                         ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(text_color));

                    if (ImGui.MenuItem("Save") && mSelectedCourseScene != null)
                    {
                        //Ensure the romfs path is set for saving
                        if (!string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                            mSelectedCourseScene.Save();
                        else //Else configure the mod path
                        {
                            FolderDialog dlg = new FolderDialog();
                            if (dlg.ShowDialog("Select the romfs directory to save to."))
                            {
                                UserSettings.SetModRomFSPath(dlg.SelectedPath);
                                mSelectedCourseScene.Save();
                            }
                        }
                    }
                    if (ImGui.MenuItem("Save As") && mSelectedCourseScene != null)
                    {
                        FolderDialog dlg = new FolderDialog();
                        if (dlg.ShowDialog("Select the romfs directory to save to."))
                        {
                            UserSettings.SetModRomFSPath(dlg.SelectedPath);
                            mSelectedCourseScene.Save();
                        }
                    }

                    ImGui.PopStyleColor();

                    /* a ImGUI menu item that just closes the application */
                    if (ImGui.MenuItem("Close"))
                    {
                        mWindow.Close();
                    }

                    /* end File menu */
                    ImGui.EndMenu();
                }
                /* end entire menu bar */
                ImGui.EndMenuBar();
            }
        }

        void DrawCourseList()
        {
            bool status = ImGui.Begin("Select Course");

            mCurrentCourseName = mSelectedCourseScene?.GetCourse().GetName();

            foreach (KeyValuePair<string, string[]> worldCourses in RomFS.GetCourseEntries())
            {
                if (ImGui.TreeNode(worldCourses.Key))
                {
                    foreach (var courseLocation in worldCourses.Value)
                    {
                        if (ImGui.RadioButton(
                                courseLocation,
                                mCurrentCourseName == null ? false : courseLocation == mCurrentCourseName
                            )
                        )
                        {
                            // Close course selection whether or not this is a different course
                            mIsChoosingCourse = false;

                            // Only change the course if it is different from current
                            if (mCurrentCourseName == null || mCurrentCourseName != courseLocation)
                            {
                                mSelectedCourseScene = new(new(courseLocation));
                                UserSettings.AppendRecentCourse(courseLocation);
                            }

                        }
                    }
                    ImGui.TreePop();
                }
            }

            if (status)
            {
                ImGui.End();
            }
        }

        public void Render(GL gl, double delta, ImGuiController controller)
        {

            /* keep OpenGLs viewport size in sync with the window's size */
            gl.Viewport(mWindow.FramebufferSize);

            gl.ClearColor(.45f, .55f, .60f, 1f);
            gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            ImGui.DockSpaceOverViewport();

            //only works after the first frame
            if (ImGui.GetFrameCount() == 2)
            {
                ImGui.LoadIniSettingsFromDisk("imgui.ini");
                LoadFromSettings();
            }

            DrawMainMenu();

            // ImGui settings are available frame 3
            if (ImGui.GetFrameCount() > 2)
            {
                if (!string.IsNullOrEmpty(RomFS.GetRoot()) && 
                    !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                {
                    if (mIsChoosingCourse)
                    {
                        DrawCourseList();
                    }

                    mSelectedCourseScene?.DrawUI();
                }

                if (misChoosingPreferences)
                {
                    Preferences.Draw(ref misChoosingPreferences);
                }
            }

            /* render our ImGUI controller */
            controller.Render();
        }

        readonly IWindow mWindow;
        string? mCurrentCourseName;
        CourseScene? mSelectedCourseScene;
        bool mIsChoosingCourse = true;
        bool misChoosingPreferences = true;
    }
}
