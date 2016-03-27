﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
    More info at 
     - https://github.com/RolandKoenig/SeeingSharp (sourcecode)
     - http://www.rolandk.de/wp (the autors homepage, german)
    Copyright (C) 2016 Roland König (RolandK)
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see http://www.gnu.org/licenses/.
*/
#endregion
using RKRocket.Game;
using SeeingSharp.Gaming;
using SeeingSharp.Infrastructure;
using SeeingSharp.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RKRocket.ViewModel
{
    public class MainUIViewModel : ViewModelBase
    {
        #region core game object
        private GameCore m_game;
        #endregion

        #region Data
        private GameDataContainer m_gameData;
        #endregion

        #region View variables
        private bool m_isPaneOpened;
        #endregion

        #region captured state variables
        private int m_currentLevel;
        private int m_currentScore;
        private int m_currentHealth;
        private bool m_isGameOver;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MainUIViewModel"/> class.
        /// </summary>
        public MainUIViewModel()
        {
            m_currentLevel = 1;
            m_currentHealth = Constants.SIM_ROCKET_MAX_HEALTH;

            // Create core game object
            if (SeeingSharpApplication.IsInitialized)
            {
                // Load current game data
                m_gameData = SeeingSharpApplication.Current.GetSingleton<GameDataContainer>();
                m_game = SeeingSharpApplication.Current.GetSingleton<GameCore>();

                // Attach to all messages 
                SeeingSharpApplication.Current.UIMessenger.SubscribeAll(this);

                this.CommandNewGame = new DelegateCommand(OnCommandNewGame_Execute);
                this.CommandSwitchPaneVisibility = new DelegateCommand(OnCommandSwitchPaneVisibility_Execute);
            }
        }

        private void OnCommandNewGame_Execute()
        {
            m_game.Scene.Messenger.BeginPublish<MessageNewGame>();

            this.IsPaneVisible = false;
        }

        private void OnCommandSwitchPaneVisibility_Execute()
        {
            this.IsPaneVisible = !this.IsPaneVisible;
        }

        private void OnMessage_Received(MessageLevelStarted message)
        {
            this.CurrentLevel = message.LevelNumber;
        }

        private void OnMessage_Received(MessageScoreChanged message)
        {
            this.CurrentScore = message.NewScore;
        }

        private void OnMessage_Received(MessagePlayerHealthChanged message)
        {
            this.CurrentHealth = message.NewHealth;
        }

        /// <summary>
        /// Called when GameOver events comes from the game.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnMessage_Received(MessageGameOver message)
        {
            if (m_isGameOver) { return; }
            m_isGameOver = true;

            base.Messenger.Publish(
                new MessageGameOverDialogRequest(
                    new GameOverViewModel(message.Reason, m_currentScore)));
        }

        private void OnMessage_Received(MessageHighScorePosted message)
        {
            this.RaisePropertyChanged(() => this.MaxReachedScore);
        }

        /// <summary>
        /// Called when the game starts from the beginning.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnMessage_Received(MessageNewGame message)
        {
            m_isGameOver = false;
            m_currentLevel = 1;
            m_currentHealth = Constants.SIM_ROCKET_MAX_HEALTH;
            m_currentScore = 0;
        }

        public GameCore Game
        {
            get { return m_game; }
        }

        /// <summary>
        /// The number of the current level.
        /// </summary>
        public int CurrentLevel
        {
            get { return m_currentLevel; }
            private set
            {
                if(m_currentLevel != value)
                {
                    m_currentLevel = value;
                    base.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the current score value reached.
        /// </summary>
        public int CurrentScore
        {
            get { return m_currentScore; }
            private set
            {
                if(m_currentScore != value)
                {
                    m_currentScore = value;
                    base.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the current health value of the player.
        /// </summary>
        public int CurrentHealth
        {
            get { return m_currentHealth; }
            set
            {
                if(m_currentHealth != value)
                {
                    m_currentHealth = value;
                    base.RaisePropertyChanged();
                }
            }
        }

        public bool IsPaneVisible
        {
            get { return m_isPaneOpened; }
            set
            {
                if(m_isPaneOpened != value)
                {
                    m_isPaneOpened = value;
                    RaisePropertyChanged();

                    if (value) { base.Messenger.Publish<MessageMenuOpened>(); }
                    else { base.Messenger.Publish<MessageMenuClosed>(); }
                }
            }
        }

        public int MaxReachedScore
        {
            get
            {
                if(m_gameData == null) { return 0; }
                if(m_gameData.GameScores.Count == 0) { return 0; }
                return m_gameData.GameScores.Max((actScore) => actScore.TotalScore);
            }
        }

        public GameDataContainer GameData
        {
            get { return m_gameData; }
            set
            {
                if(m_gameData != value)
                {
                    m_gameData = value;

                    base.RaisePropertyChanged();
                    base.RaisePropertyChanged(() => MaxReachedScore);
                }
            }
        }

        /// <summary>
        /// Gets the command that starts a new game.
        /// </summary>
        public DelegateCommand CommandNewGame
        {
            get;
            private set;
        }

        public DelegateCommand CommandSwitchPaneVisibility
        {
            get;
            private set;
        }
    }
}
