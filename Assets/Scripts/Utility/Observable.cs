using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

public class Observable<T>
{
    private T _value;
    public event Action<T> OnValueChanged;
    public event Action<T> OnValueSet;

    public T Value
    {
        get
        {
            return _value;
        }
        set
        {

            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
            _value = value;
            OnValueSet?.Invoke(_value);

        }

    }

    public Observable()
    {

    }
    public Observable(T initial)
    {
        _value = initial;
    }
}