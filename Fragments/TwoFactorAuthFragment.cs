﻿using System;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Madamin.Unfollow.Instagram;

namespace Madamin.Unfollow.Fragments
{
    public class TwoFactorAuthFragment : FragmentBase
    {
        private readonly Account _account;

        private MaterialButton _btnVerify, _btnResend;
        private TextInputEditText _textInput;

        private bool _didChallenge;

        public TwoFactorAuthFragment(Account account) :
            base(Resource.Layout.fragment_login_2fa)
        {
            _account = account;

            Create += TwoFactorAuthFragment_Create;
        }

        private void TwoFactorAuthFragment_Create(object sender, OnFragmentCreateEventArgs e)
        {
            if (_didChallenge)
            {
                PopFragment();
            }

            // Fragment setup
            Title = GetString(Resource.String.title_2fa);
            ActionBarVisible = false;

            // Find views
            _textInput = e.View.FindViewById<TextInputEditText>(Resource.Id.fragment_login_2fa_code_input);
            _btnResend = e.View.FindViewById<MaterialButton>(Resource.Id.fragment_login_2fa_resend);
            _btnVerify = e.View.FindViewById<MaterialButton>(Resource.Id.fragment_login_2fa_verify);

            if (_btnVerify == null ||
                _btnResend == null)
                return;

            // Setup click handlers
            _btnVerify.Click += VerifyButton_OnClick;
            _btnResend.Click += ResendTextView_OnClick;
        }

        private async void VerifyButton_OnClick(object sender, EventArgs e)
        {
            _textInput.Enabled = false;
            _btnResend.Enabled = false;
            _btnVerify.Enabled = false;
            try
            {
                var instagram = ((IInstagramHost) Activity).Accounts;
                await instagram.CompleteLoginAsync(
                    _account,
                    _textInput.Text);

#if TGBUILD || DEBUG
                ((IUpdateServerHost) Activity).DidLogin();
#endif

                PopFragment();
            }
            catch (ChallengeException ex)
            {
                var challengeFragment = new ChallengeFragment(ex.Account);
                PushFragment(challengeFragment);
                _didChallenge = true;
            }
            catch (InvalidTwoFactorCodeException)
            {
                ((ISnackBarHost)Activity).ShowSnackbar(Resource.String.error_invalid_2fa);
            }
            catch (TwoFactorCodeExpiredException)
            {
                ((ISnackBarHost)Activity).ShowSnackbar(Resource.String.error_2fa_expired);
            }
            catch (Exception ex)
            {
                ((IErrorHost) Activity).ShowError(ex);
            }
            finally
            {
                _textInput.Enabled = true;
                _btnResend.Enabled = true;
                _btnVerify.Enabled = true;
            }
        }

        private async void ResendTextView_OnClick(object sender, EventArgs e)
        {
            _btnResend.Enabled = false;
            try
            {
                await _account.TwoFactorSendSms();
            }
            catch (Exception ex)
            {
                _btnResend.Enabled = true;
                ((IErrorHost) Activity).ShowError(ex);
            }
        }
    }
}