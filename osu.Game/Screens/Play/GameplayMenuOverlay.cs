﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Play
{
    public abstract partial class GameplayMenuOverlay : OverlayContainer, IKeyBindingHandler<GlobalAction>
    {
        protected const int TRANSITION_DURATION = 200;

        private const int button_height = 70;
        private const float background_alpha = 0.75f;

        protected override bool BlockNonPositionalInput => true;

        protected override bool BlockScrollInput => false;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        public Action OnRetry;
        public Action OnQuit;

        /// <summary>
        /// Action that is invoked when <see cref="GlobalAction.Back"/> is triggered.
        /// </summary>
        protected virtual Action BackAction => () => InternalButtons.LastOrDefault()?.TriggerClick();

        /// <summary>
        /// Action that is invoked when <see cref="GlobalAction.Select"/> is triggered.
        /// </summary>
        protected virtual Action SelectAction => () => InternalButtons.Selected?.TriggerClick();

        public abstract string Header { get; }

        public abstract string Description { get; }

        protected SelectionCycleFillFlowContainer<DialogButton> InternalButtons;
        public IReadOnlyList<DialogButton> Buttons => InternalButtons;

        private FillFlowContainer retryCounterContainer;

        protected GameplayMenuOverlay()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = background_alpha,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 50),
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 20),
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = Header,
                                    Font = OsuFont.GetFont(size: 30),
                                    Spacing = new Vector2(5, 0),
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Colour = colours.Yellow,
                                    Shadow = true,
                                    ShadowColour = new Color4(0, 0, 0, 0.25f)
                                },
                                new OsuSpriteText
                                {
                                    Text = Description,
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Shadow = true,
                                    ShadowColour = new Color4(0, 0, 0, 0.25f)
                                }
                            }
                        },
                        InternalButtons = new SelectionCycleFillFlowContainer<DialogButton>
                        {
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Masking = true,
                            EdgeEffect = new EdgeEffectParameters
                            {
                                Type = EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.6f),
                                Radius = 50
                            },
                        },
                        retryCounterContainer = new FillFlowContainer
                        {
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            AutoSizeAxes = Axes.Both,
                        }
                    }
                },
            };

            State.ValueChanged += _ => InternalButtons.Deselect();

            updateRetryCount();
        }

        private int retries;

        public int Retries
        {
            set
            {
                if (value == retries)
                    return;

                retries = value;
                if (retryCounterContainer != null)
                    updateRetryCount();
            }
        }

        protected override void PopIn() => this.FadeIn(TRANSITION_DURATION, Easing.In);
        protected override void PopOut() => this.FadeOut(TRANSITION_DURATION, Easing.In);

        // Don't let mouse down events through the overlay or people can click circles while paused.
        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnMouseMove(MouseMoveEvent e) => true;

        protected void AddButton(string text, Color4 colour, Action action)
        {
            var button = new Button
            {
                Text = text,
                ButtonColour = colour,
                Origin = Anchor.TopCentre,
                Anchor = Anchor.TopCentre,
                Height = button_height,
                Action = delegate
                {
                    action?.Invoke();
                    Hide();
                }
            };

            InternalButtons.Add(button);
        }

        public virtual bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.SelectPrevious:
                    InternalButtons.SelectPrevious();
                    return true;

                case GlobalAction.SelectNext:
                    InternalButtons.SelectNext();
                    return true;

                case GlobalAction.Back:
                    BackAction.Invoke();
                    return true;

                case GlobalAction.Select:
                    SelectAction.Invoke();
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        private void updateRetryCount()
        {
            // "You've retried 1,065 times in this session"
            // "You've retried 1 time in this session"

            retryCounterContainer.Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = "You've retried ",
                    Shadow = true,
                    ShadowColour = new Color4(0, 0, 0, 0.25f),
                    Font = OsuFont.GetFont(size: 18),
                },
                new OsuSpriteText
                {
                    Text = "time".ToQuantity(retries),
                    Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 18),
                    Shadow = true,
                    ShadowColour = new Color4(0, 0, 0, 0.25f),
                },
                new OsuSpriteText
                {
                    Text = " in this session",
                    Shadow = true,
                    ShadowColour = new Color4(0, 0, 0, 0.25f),
                    Font = OsuFont.GetFont(size: 18),
                }
            };
        }

        private partial class Button : DialogButton
        {
            // required to ensure keyboard navigation always starts from an extremity (unless the cursor is moved)
            protected override bool OnHover(HoverEvent e) => true;

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                State = SelectionState.Selected;
                return base.OnMouseMove(e);
            }
        }

        [Resolved]
        private GlobalActionContainer globalAction { get; set; }

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case ScrollEvent:
                    if (ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
                        return globalAction.TriggerEvent(e);

                    break;
            }

            return base.Handle(e);
        }
    }
}
