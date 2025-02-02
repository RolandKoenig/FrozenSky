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
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeeingSharp.Multimedia.Core;
using SeeingSharp.Util;
using System.Threading;
using System.Diagnostics;
using SeeingSharp.Multimedia.Objects;
using Xunit;

namespace SeeingSharp.Tests.Rendering
{
    public class AnimationSystemTests
    {
        private const string TEST_CATEGORY = "SeeingSharp Multimedia Animation";

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_1()
        {
            // Define animation parameters / objects
            DummyAnimatableObject animateableObject = new DummyAnimatableObject();
            int dummyInt = 0;

            // Define animation sequence
            //  .. nothing special, just step by step
            animateableObject.BuildAnimationSequence()
                .ChangeFloatBy(
                    () => animateableObject.DummyValue,
                    (givenValue) => animateableObject.DummyValue = givenValue,
                    10f,
                    TimeSpan.FromMilliseconds(600.0))
                .WaitFinished()
                .CallAction(() =>
                {
                    dummyInt = 10;
                })
                .Apply();

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = animateableObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 2);
            Assert.True(dummyInt == 10);
            Assert.True(animateableObject.DummyValue == 10f);
            Assert.True(passInfo.TotalTime == TimeSpan.FromMilliseconds(600.0));
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_2()
        {
            // Define animation parameters / objects
            DummyAnimatableObject animateableObject = new DummyAnimatableObject();
            int dummyInt = 0;

            // Define animation sequence
            //  .. nothing special, just step by step
            animateableObject.BuildAnimationSequence()
                .ChangeFloatBy(
                    () => animateableObject.DummyValue,
                    (givenValue) => animateableObject.DummyValue = givenValue,
                    10f,
                    TimeSpan.FromMilliseconds(500.0))
                .WaitFinished()
                .CallAction(() =>
                {
                    dummyInt = 10;
                })
                .ChangeFloatBy(
                    () => animateableObject.DummyValue,
                    (givenValue) => animateableObject.DummyValue = givenValue,
                    20f,
                    TimeSpan.FromMilliseconds(500.0))
                .Apply();

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = animateableObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 2);
            Assert.True(dummyInt == 10);
            Assert.True(animateableObject.DummyValue == 30f);
            Assert.True(passInfo.TotalTime == TimeSpan.FromSeconds(1.0));
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_3()
        {
            // Define animation parameters / objects
            GenericObject genObject = new GenericObject(NamedOrGenericKey.Empty);

            // Define animation sequence
            //  .. use 3D-Engine GenericObject (animated just like simulation objects)
            genObject.BuildAnimationSequence()
                .Move3DBy(new Vector3(5f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromMilliseconds(750.0))
                .WaitFinished()
                .Apply();

            // Main-Test: Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = genObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 1);
            Assert.True(passInfo.Steps[0].UpdateTime == TimeSpan.FromMilliseconds(750.0));
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_4()
        {
            // Define animation parameters / objects
            GenericObject genObject = new GenericObject(NamedOrGenericKey.Empty);

            // Define animation sequence
            //  .. A bit more complicated.. this method uses a fixed wait timer in between. Not all animations before it would be finished
            genObject.BuildAnimationSequence()
                .Move3DBy(new Vector3(5f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromMilliseconds(750.0))
                .WaitUntilTimePassed(TimeSpan.FromMilliseconds(600.0))
                .RotateEulerAnglesBy(new Vector3(2f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .WaitFinished()
                .Apply();

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = genObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 2);
            Assert.True(passInfo.Steps[0].UpdateTime == TimeSpan.FromMilliseconds(600.0));
            Assert.True(passInfo.Steps[1].UpdateTime == TimeSpan.FromMilliseconds(500.0));
            Assert.True(passInfo.TotalTime == TimeSpan.FromSeconds(1.1));
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_5()
        {
            // Define animation parameters / objects
            GenericObject genObject = new GenericObject(NamedOrGenericKey.Empty);

            // Define animation sequence
            //  .. A bit more complicated.. this method uses a fixed wait timer in between. Not all animations before it would be finished
            //  More complicated here: The step "Scale3DTo" is the longest and controls the duration of the second event
            genObject.BuildAnimationSequence()
                .Move3DBy(new Vector3(5f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromMilliseconds(1750.0))
                .WaitUntilTimePassed(TimeSpan.FromMilliseconds(600.0))
                .RotateEulerAnglesBy(new Vector3(2f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .WaitFinished()
                .Apply();

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = genObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 2);
            Assert.True(passInfo.Steps[0].UpdateTime == TimeSpan.FromMilliseconds(600.0));
            Assert.True(passInfo.Steps[1].UpdateTime == TimeSpan.FromMilliseconds(1150.0));
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_6()
        {
            // Define animation parameters / objects
            GenericObject genObject = new GenericObject(NamedOrGenericKey.Empty);

            // Define animation sequence
            //  Same as above, just with a wait-finished event here (dynamic wait step)
            genObject.BuildAnimationSequence()
                .Move3DBy(new Vector3(5f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromMilliseconds(1750.0))
                .WaitFinished()
                .RotateEulerAnglesBy(new Vector3(2f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .WaitFinished()
                .Apply();

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = genObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 2);
            Assert.True(passInfo.Steps[0].UpdateTime == TimeSpan.FromMilliseconds(1750.0));
            Assert.True(passInfo.Steps[1].UpdateTime == TimeSpan.FromMilliseconds(500.0));
        }


        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_7()
        {
            // Define animation parameters / objects
            GenericObject genObject = new GenericObject(NamedOrGenericKey.Empty);

            // Define animation sequence
            //  Same as above, just with a wait-finished event here (dynamic wait step)
            genObject.BuildAnimationSequence()
                .Move3DBy(new Vector3(5f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromMilliseconds(1750.0))
                .WaitFinished()
                .RotateEulerAnglesBy(new Vector3(2f, 0f, 0f), TimeSpan.FromMilliseconds(500.0))
                .WaitFinished()
                .Apply();
            genObject.BuildAnimationSequence()
                .Move3DBy(new Vector3(5f, 0f, 0f), TimeSpan.FromMilliseconds(400.0))
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromMilliseconds(1850.0))
                .WaitFinished()
                .RotateEulerAnglesBy(new Vector3(2f, 0f, 0f), TimeSpan.FromMilliseconds(300.0))
                .WaitFinished()
                .ApplyAsSecondary();

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = genObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 4);
            Assert.True(passInfo.Steps[0].UpdateTime == TimeSpan.FromMilliseconds(1750.0));
            Assert.True(passInfo.Steps[1].UpdateTime == TimeSpan.FromMilliseconds(100.0));
            Assert.True(passInfo.Steps[2].UpdateTime == TimeSpan.FromMilliseconds(300.0));
            Assert.True(passInfo.Steps[3].UpdateTime == TimeSpan.FromMilliseconds(100.0));
        }

        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_8()
        {
            // Define animation parameters / objects
            GenericObject genObject = new GenericObject(NamedOrGenericKey.Empty);

            // Define animation sequence
            //  Same as above, just with a wait-finished event here (dynamic wait step)
            genObject.BuildAnimationSequence()
                .Move3DTo(new Vector3(6f, 0f, 0f), new MovementSpeed(0.3f))                      // 20,0 Secs
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromSeconds(2.0))                   // 02,0 Secs
                .WaitFinished()
                .Move3DTo(new Vector3(9f, 0f, 0f), new MovementSpeed(0.3f))                      // 10,0 Secs
                .RotateEulerAnglesBy(new Vector3(2f, 0f, 0f), TimeSpan.FromSeconds(12.0))        // 12,0 Secs
                .WaitFinished()
                .Apply();
            genObject.BuildAnimationSequence()
                .Move3DTo(new Vector3(10f, 0f, 0f), new MovementSpeed(0.4f))                     // 25,0 Secs
                .Scale3DTo(new Vector3(2f, 2f, 2f), TimeSpan.FromSeconds(30.0))                  // 30,0 Secs
                .WaitFinished()
                .Move3DTo(new Vector3(11f, 0f, 0f), new MovementSpeed(0.4f))                     //  2,5 Secs      
                .RotateEulerAnglesBy(new Vector3(2f, 0f, 0f), TimeSpan.FromSeconds(1.0))         //  1,0 Secs
                .WaitFinished()
                .ApplyAsSecondary();

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = genObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 4);
            Assert.True(passInfo.Steps[0].UpdateTime == TimeSpan.FromSeconds(20.0));
            Assert.True(passInfo.Steps[1].UpdateTime == TimeSpan.FromSeconds(10.0));
            Assert.True(passInfo.Steps[2].UpdateTime == TimeSpan.FromSeconds(2.0));
            Assert.True(passInfo.Steps[3].UpdateTime == TimeSpan.FromSeconds(0.5));
            Assert.True(Math.Round(passInfo.TotalTime.TotalSeconds, 1) == 32.5);
        }

        /// <summary>
        /// Checks for correct empty states of the AnimationHandler.
        /// </summary>
        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public void Test_EventDriven_SimpleAnimation_EmptyStates()
        {
            // Define animation parameters / objects
            GenericObject genObject = new GenericObject(NamedOrGenericKey.Empty);

            Assert.True(genObject.AnimationHandler.TimeTillCurrentAnimationStepFinished == new TimeSpan(0, 0, 0, 0, int.MaxValue));

            // Perform animation in an event-driven way
            EventDrivenPassInfo passInfo = genObject.AnimationHandler.CalculateEventDriven();

            // Check results
            Assert.True(passInfo.CountSteps == 0);
            Assert.True(genObject.AnimationHandler.TimeTillCurrentAnimationStepFinished == new TimeSpan(0, 0, 0, 0, int.MaxValue));
        }

        /// <summary>
        /// Tests the simple animation.
        /// </summary>
        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Test_Continued_SimpleAnimation_1()
        {
            int dummyInt = 0;
            float dummyFloat = 0;

            using (CancellationTokenSource cancelTokenSource = new CancellationTokenSource())
            {
                DummyAnimatableObject animateableObject = new DummyAnimatableObject();

                TplBasedLoop animLoop = new TplBasedLoop(0);
                animLoop.Tick += (sender, eArgs) => animateableObject.Update(new UpdateState(TimeSpan.FromMilliseconds(1.0)));
                animLoop.RunAsync(cancelTokenSource.Token)
                    .FireAndForget();

                await animateableObject.BuildAnimationSequence()
                    .ChangeFloatBy(
                        () => animateableObject.DummyValue,
                        (givenValue) => animateableObject.DummyValue = givenValue,
                        10f,
                        TimeSpan.FromMilliseconds(500.0))
                    .WaitFinished()
                    .CallAction(() =>
                    {
                        dummyInt = 10;
                        dummyFloat = animateableObject.DummyValue;
                    })
                    .ApplyAsync();

                Assert.True(dummyInt == 10);
                Assert.True(dummyFloat == 10f);
            }
        }

        /// <summary>
        /// Tests multiple animations in parallel.
        /// </summary>
        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Test_Continued_MultipleAnimations_1()
        {
            int dummyInt = 0;
            float dummyFloat = 0;
            DummyAnimatableObject animateableObject = new DummyAnimatableObject();
            List<Task> allTasks = new List<Task>();

            using (CancellationTokenSource cancelTokenSource = new CancellationTokenSource())
            {
                TplBasedLoop animLoop = new TplBasedLoop(0);
                animLoop.Tick += (sender, eArgs) => animateableObject.Update(new UpdateState(TimeSpan.FromMilliseconds(1)));
                animLoop.RunAsync(cancelTokenSource.Token)
                    .FireAndForget();

                for (int loop = 0; loop < 20; loop++)
                {
                    allTasks.Add(animateableObject.BuildAnimationSequence()
                        .ChangeFloatBy(
                            () => animateableObject.DummyValue,
                            (givenValue) => animateableObject.DummyValue = givenValue,
                            10f,
                            TimeSpan.FromMilliseconds(100.0))
                        .WaitFinished()
                        .CallAction(() =>
                        {
                            Interlocked.Increment(ref dummyInt);
                            dummyFloat = animateableObject.DummyValue;
                        })
                        .ApplyAsync());
                }

                await Task.WhenAll(allTasks);
            }

            Assert.True(dummyInt == 20);
        }

        /// <summary>
        /// Tests multiple animations in parallel.
        /// </summary>
        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Test_Continued_MultipleAnimations_2()
        {
            const int COUNT_CYCLES = 1000;

            int dummyInt = 0;
            float dummyFloat = 0;
            DummyAnimatableObject animateableObject = new DummyAnimatableObject();
            List<Task> allTasks = new List<Task>();

            using (CancellationTokenSource cancelTokenSource = new CancellationTokenSource())
            {
                TplBasedLoop animLoop = new TplBasedLoop(0);
                animLoop.Tick += (sender, eArgs) => animateableObject.Update(new UpdateState(TimeSpan.FromMilliseconds(1)));
                animLoop.RunAsync(cancelTokenSource.Token)
                    .FireAndForget();

                for (int loop = 0; loop < COUNT_CYCLES; loop++)
                {
                    allTasks.Add(animateableObject.BuildAnimationSequence()
                        .ChangeFloatBy(
                            () => animateableObject.DummyValue,
                            (givenValue) => animateableObject.DummyValue = givenValue,
                            10f,
                            TimeSpan.FromMilliseconds(1500.0))
                        .WaitFinished()
                        .CallAction(() =>
                        {
                            Interlocked.Increment(ref dummyInt);
                            dummyFloat = animateableObject.DummyValue;
                        })
                        .ApplyAsync());
                    if (loop % 10 == 0) { Thread.Sleep(1); }
                }

                await Task.WhenAll(allTasks);
            }

            Assert.True(dummyInt == COUNT_CYCLES);
        }

        /// <summary>
        /// This test checks functionality of the BeginCancelAnimation call done on started animations.
        /// </summary>
        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Test_Continued_CancelAnimations_1()
        {
            const int COUNT_CYCLES = 500;

            // Some variables for preparation
            int dummyInt = 0;
            float dummyFloat = 0;
            DummyAnimatableObject animateableObject = new DummyAnimatableObject();
            List<Task> allTasks = new List<Task>();
            AggregateException aggregateException = null;
            TaskCanceledException cancelException = null;
            TimeSpan animDuration = TimeSpan.MaxValue;

            using (CancellationTokenSource cancelTokenSource = new CancellationTokenSource())
            {
                // Creates a dummy loop which updates our animation using a background thread
                TplBasedLoop animLoop = new TplBasedLoop(0);
                animLoop.Tick += (sender, eArgs) => animateableObject.Update(new UpdateState(TimeSpan.FromMilliseconds(1)));
                animLoop.RunAsync(cancelTokenSource.Token)
                    .FireAndForget();

                // Define a high count of animations
                for (int loop = 0; loop < COUNT_CYCLES; loop++)
                {
                    allTasks.Add(animateableObject.BuildAnimationSequence()
                        .ChangeFloatBy(
                            () => animateableObject.DummyValue,
                            (givenValue) => animateableObject.DummyValue = givenValue,
                            10f,
                            TimeSpan.FromMilliseconds(15000.0))
                        .WaitFinished()
                        .CallAction(() =>
                        {
                            Interlocked.Increment(ref dummyInt);
                            dummyFloat = animateableObject.DummyValue;
                        })
                        .ApplyAsync());
                    if (loop % 10 == 0) { Thread.Sleep(1); }
                }
                animateableObject.AnimationHandler.BeginCancelAnimation();

                // This has to be false here because animations get canceled
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                {
                    try
                    {
                        await Task.WhenAll(allTasks);
                    }
                    catch(TaskCanceledException exCancel)
                    {
                        // This exception is expected because of the BeginCancelAnimation call
                        cancelException = exCancel;
                    }
                    catch (AggregateException ex)
                    {
                        // This exception is expected because of the BeginCancelAnimation call
                        aggregateException = ex;
                        cancelException = ex.InnerException as TaskCanceledException;
                    }
                }
                stopwatch.Stop();
                animDuration = stopwatch.Elapsed;
            }

            // Checks after animation processing
            Assert.True(dummyInt < COUNT_CYCLES, "Wrong count of cycles!");
            Assert.True(animDuration < TimeSpan.FromMilliseconds(500.0), "Animation was not cancelled correctly!");
            Assert.NotNull(cancelException);
        }

        /// <summary>
        /// Tests secondary animtions functionality.
        /// </summary>
        [Fact]
        [Trait("Category", TEST_CATEGORY)]
        public async Task Test_Continued_SecondaryAnimations_1()
        {
            const int COUNT_CYCLES = 500;

            int dummyInt = 0;
            float dummyFloat = 0;
            DummyAnimatableObject animateableObject = new DummyAnimatableObject();
            List<Task> allTasks = new List<Task>();
            TaskCanceledException cancelEx = null;

            using (CancellationTokenSource cancelTokenSource = new CancellationTokenSource())
            {
                // Start dummy-loop which updates our object
                TplBasedLoop animLoop = new TplBasedLoop(0);
                animLoop.Tick += (sender, eArgs) => animateableObject.Update(new UpdateState(TimeSpan.FromMilliseconds(1)));
                animLoop.RunAsync(cancelTokenSource.Token)
                    .FireAndForget();

                // Start all animations
                for (int loop = 0; loop < COUNT_CYCLES; loop++)
                {
                    allTasks.Add(animateableObject.BuildAnimationSequence()
                        .ChangeFloatBy(
                            () => animateableObject.DummyValue,
                            (givenValue) => animateableObject.DummyValue = givenValue,
                            10f,
                            TimeSpan.FromMilliseconds(15000.0))
                        .WaitFinished()
                        .CallAction(() =>
                        {
                            Interlocked.Increment(ref dummyInt);
                            dummyFloat = animateableObject.DummyValue;
                        })
                        .ApplyAsync());
                    if (loop % 10 == 0) { Thread.Sleep(1); }
                }

                // Create a secondary animation (works in parallel)
                await animateableObject.BuildAnimationSequence()
                   .ChangeFloatBy(
                       () => animateableObject.DummyValue2,
                       (givenValue) => animateableObject.DummyValue2 = givenValue,
                       10f,
                       TimeSpan.FromMilliseconds(500.0))
                   .ApplyAsSecondaryAsync();

                // Now cancel all animations
                animateableObject.AnimationHandler.BeginCancelAnimation();

                // Wait for all tasks
                try
                {
                    await Task.WhenAll(allTasks);
                }
                catch(TaskCanceledException ex)
                {
                    cancelEx = ex;
                }
            }

            Assert.True(cancelEx is TaskCanceledException, "Some unexpected exception was raised!");
            Assert.True(dummyInt < COUNT_CYCLES, "Wrong count of cycles!");
            Assert.True(animateableObject.DummyValue2 == 10f, "Secondary animation has not processed correctly!");
        }

        //*********************************************************************
        //*********************************************************************
        //*********************************************************************
        private class DummyAnimatableObject : IAnimatableObject
        {
            private AnimationHandler m_animHandler;

            public DummyAnimatableObject()
            {
                m_animHandler = new AnimationHandler(this);
            }

            /// <summary>
            /// Updates this object.
            /// </summary>
            /// <param name="updateState">The current state of the update process.</param>
            public void Update(UpdateState updateState)
            {
                m_animHandler.Update(updateState);
            }

            public AnimationHandler AnimationHandler
            {
                get { return m_animHandler; }
            }

            public float DummyValue
            {
                get;
                set;
            }

            public float DummyValue2
            {
                get;
                set;
            }
        }
    }
}
