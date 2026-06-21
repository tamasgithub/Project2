using System;
using UnityEngine;

public  class TemporaryEffect
{
    public Action OnApply { get; private set; }
    public Action OnTick { get; private set; }
    public Action OnRemove { get; private set; }
    public bool IsComplete { get => _elapsedTime >= _duration; }
    private float _duration = 0.0f;
    private float _elapsedTime = 0f;
    private float _nextTick = 0.0f;
    private float _tickRate = 1;
    private int _totalTicks = 0;
    private int _maxTicks = 100000;
    public TemporaryEffect(float duration)
    {
        _duration = duration;
    }
    public void Update(float deltaTime)
    {
        _elapsedTime += deltaTime;
        if (_totalTicks >= _maxTicks) return;
        _nextTick += deltaTime;
        if (OnTick != null && _nextTick >= _tickRate)
        {
            _nextTick -= _tickRate;
            OnTick();
            _totalTicks++;
        }
        
    }

    public TemporaryEffect IsBleed(Entity target)
    {

        this.OnTick = () => target.ReceiveDamage(Math.Max(1, (int)((float)(target.MaxHp) / (float)(GlobalConstants.BLEED_BASE_PERCENTAGE))));
        return this;
    }
    public TemporaryEffect SetOnApply(Action action)
    {
        this.OnApply = action;
        return this;
    }
    public TemporaryEffect SetOnRemove(Action action)
    {
        this.OnRemove = action;
        return this;
    }
    public TemporaryEffect SetTickRate(float rate)
    {
        this._tickRate = 1f / rate;
        this._nextTick = this._tickRate;
        return this;
    }
    public TemporaryEffect SetOnTick(Action action)
    {
        this.OnTick = action;
        return this;
    }
    public TemporaryEffect SetMaxTicks(int max)
    {
        this._maxTicks = max;
        return this;
    }
    

}