//using System;


//namespace Acr.Tests
//{
//    class RxTests
//    {
//        static readonly Func<int, TimeSpan> LinearMsStrategy = n => TimeSpan.FromMilliseconds(1 * n);

//        [Fact]
//        public void DoesNotRetryInCaseOfSuccess()
//        {
//            new TestScheduler().With(sched =>
//            {
//                int tryCount = 0;

//                var source = Observable.Defer(() =>
//                {
//                    tryCount++;
//                    return Observable.Return("yolo");
//                });

//                source.RetryWithBackoffStrategy(
//                    retryCount: 3,
//                    strategy: LinearMsStrategy,
//                    scheduler: sched
//                    );

//                source.Subscribe();

//                Assert.Equal(1, tryCount);

//                sched.AdvanceByMs(1);
//                Assert.Equal(1, tryCount);
//            });
//        }

//        [Fact]
//        public void PropagatesLastObservedExceptionIfAllTriesFail()
//        {
//            new TestScheduler().With(sched =>
//            {
//                int tryCount = 0;

//                var source = Observable.Defer(() =>
//                {
//                    tryCount++;
//                    return Observable.Throw<string>(new InvalidOperationException(tryCount.ToString()));
//                });

//                var observable = source.RetryWithBackoffStrategy(
//                    retryCount: 3,
//                    strategy: LinearMsStrategy,
//                    scheduler: sched
//                    );

//                Exception lastError = null;
//                observable.Subscribe(_ => { }, e => { lastError = e; });

//                Assert.Equal(1, tryCount);

//                sched.AdvanceByMs(1);
//                Assert.Equal(2, tryCount);

//                sched.AdvanceByMs(2);
//                Assert.Equal(3, tryCount);

//                Assert.Null(lastError);
//                Assert.Equal("3", lastError.Message);
//            });
//        }

//        [Fact]
//        public void RetriesOnceIfSuccessBeforeRetriesRunOut()
//        {
//            new TestScheduler().With(sched =>
//            {
//                int tryCount = 0;

//                var source = Observable.Defer(() =>
//                {
//                    if (tryCount++ < 1) return Observable.Throw<string>(new InvalidOperationException());
//                    return Observable.Return("yolo " + tryCount);
//                });

//                var observable = source.RetryWithBackoffStrategy(
//                    retryCount: 5,
//                    strategy: LinearMsStrategy,
//                    scheduler: sched
//                    );

//                string lastValue = null;
//                observable.Subscribe(n => { lastValue = n; });

//                Assert.Equal(1, tryCount);
//                Assert.Null(lastValue);

//                sched.AdvanceByMs(1);
//                Assert.Equal(2, tryCount);
//                Assert.Equal("yolo 2", lastValue);
//            });
//        }

//        [Fact]
//        public void UnsubscribingDisposesSource()
//        {
//            new TestScheduler().With(sched =>
//            {
//                int c = -1;

//                var neverEndingSource = Observable.Defer(() =>
//                {
//                    return Observable.Timer(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), sched)
//                        .Do(_ => c++)
//                        .Select(_ => Unit.Default);
//                });

//                var observable = neverEndingSource.RetryWithBackoffStrategy(scheduler: sched);

//                // Cold
//                Assert.Equal(-1, c);

//                var disp = observable
//                    .Take(2)
//                    .Subscribe();

//                sched.AdvanceByMs(1);
//                Assert.Equal(0, c);

//                sched.AdvanceByMs(1);
//                Assert.Equal(1, c);

//                sched.AdvanceByMs(10);
//                Assert.Equal(1, c);
//            });
//        }
//    }
//}
