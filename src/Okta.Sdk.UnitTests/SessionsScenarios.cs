﻿// <copyright file="SessionsScenarios.cs" company="Okta, Inc">
// Copyright (c) 2014 - present Okta, Inc. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Okta.Sdk.Internal;
using Xunit;

namespace Okta.Sdk.UnitTests
{
    /// <summary>
    /// For integration tests purposes, Sessions require a complex setup which involves AuthN api, for this reason they are not provided.
    /// In these tests, we make sure that the SDK produce the expected HTTP call (URL, body) when using a Sessions API method
    /// and, the SDK correctly deserialize the expected response from the Sessions API.
    /// </summary>
    public class SessionsScenarios
    {
        private static (IRequestExecutor MockRequestExecutor, IDataStore DataStore) SetUpMocks()
        {
            var mockRequestExecutor = Substitute.For<IRequestExecutor>();

            mockRequestExecutor
                .GetAsync(Arg.Any<string>(), Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<CancellationToken>())
                .Returns(new HttpResponse<string>() { StatusCode = 200 });

            mockRequestExecutor
                .PostAsync(Arg.Any<string>(), Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new HttpResponse<string>() { StatusCode = 200 });

            mockRequestExecutor
                .PutAsync(Arg.Any<string>(), Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new HttpResponse<string>() { StatusCode = 200 });

            mockRequestExecutor
                .DeleteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<CancellationToken>())
                .Returns(new HttpResponse<string>() { StatusCode = 200 });

            var dataStore = new DefaultDataStore(
                mockRequestExecutor,
                new DefaultSerializer(),
                new ResourceFactory(null, null),
                NullLogger.Instance);

            return (mockRequestExecutor, dataStore);
        }

        [Fact]
        public async Task DelegateAValidPostToRequestExecutorGivenACreateSessionRequest()
        {
            var (mockRequestExecutor, dataStore) = SetUpMocks();
            var createSessionRequest = new CreateSessionRequest()
            {
                SessionToken = "foo",
            };

            var request = new HttpRequest
            {
                Uri = "/api/v1/sessions",
                Payload = createSessionRequest,
            };

            await dataStore.PostAsync<Session>(request, new RequestContext(), CancellationToken.None);
            await mockRequestExecutor.Received().PostAsync(
                "/api/v1/sessions",
                Arg.Any<IEnumerable<KeyValuePair<string, string>>>(),
                "{\"sessionToken\":\"foo\"}",
                CancellationToken.None);
        }

        [Fact]
        public async Task ThrowApiExceptionFor401()
        {
            var rawErrorResponse = @"
            {
            ""errorCode"": ""E0000004"",
            ""errorSummary"": ""Authentication failed"",
            ""errorLink"": ""E0000004"",
            ""errorId"": ""oaePeqyp7cuRaKQ6B95RY6Oyg"",
            ""errorCauses"": []
            }";

            var mockRequestExecutor = new MockedStringRequestExecutor(rawErrorResponse, 401);
            var client = new TestableOktaClient(mockRequestExecutor);

            try
            {
                await client.Sessions.GetSessionAsync("12345");
            }
            catch (OktaApiException apiException)
            {
                apiException.Message.Should().Be("Authentication failed (401, E0000004)");
                apiException.ErrorCode.Should().Be("E0000004");
                apiException.ErrorSummary.Should().Be("Authentication failed");
                apiException.ErrorLink.Should().Be("E0000004");
                apiException.ErrorId.Should().Be("oaePeqyp7cuRaKQ6B95RY6Oyg");
                apiException.Error.Should().NotBeNull();
            }
        }
    }
}
