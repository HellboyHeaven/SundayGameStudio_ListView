using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GridView : VisualElement
{
    #region Private Field

    private IList _itemSource;
    private Vector2 _scrollVelocity;
    private bool _startedMoving;
    private bool _touchStoppedVelocity;
    private IVisualElementScheduledItem _postPointerUpAnimation;
    private float _springSpeed = 50f;
    private Vector2 _springBackVelocity;
    private bool _waiting;
    
    private Vector2 _highBound;
    private Vector2 _lowBound ;
    private bool _canClick;
    private bool _clamp = true;
    private int _pointerId;
    private Vector2 _startPosition;
    private Vector2 _pointerStartPosition;

    #endregion

    #region Public Property
    
    public IList itemsSource
    {
        get => _itemSource;
        set
        {
            _itemSource = value;
            _waiting = false;
            Update();
        }
    }
    
    public Func<VisualElement> makeItem {get; set; }
    public Action<VisualElement, object> bindItem { get; set; }
    public Action<VisualElement, object> onSelect { get; set; }
    public Action reachEnd { get; set; }
    public bool waitToLoad { get; set; }
    public float alignmentLength =>
        (constraint == GridConstraint.ColumnFixedCount ? resolvedStyle.width : resolvedStyle.height) / constraintCount;
    public int itemCountOnScreen =>
        (int)Math.Ceiling((constraint == GridConstraint.ColumnFixedCount ? resolvedStyle.height : resolvedStyle.width) / alignmentLength) * constraintCount;
    
    #endregion
    
    #region Serialized

    public GridConstraint constraint { get; private set; }
    public int constraintCount { get; private set; }
    public int margin { get; private set; }
    private float elasticity { get; set; }
    private float scrollDecelerationRate { get; set; }
   

    #endregion
    
    #region Private Property
    private Vector2 outHighBound => _highBound + new Vector2(resolvedStyle.width * 0.1f, resolvedStyle.height * 0.1f);
    private Vector2 outLowBound => _lowBound - new Vector2(resolvedStyle.width * 0.1f, resolvedStyle.height * 0.1f);
    
   

    private Vector2 scrollOffset
    {
        get => contentContainer.transform.position;
        set
        {
            float valueX = Mathf.Clamp(value.x, (_clamp ? _lowBound.x : outLowBound.x), (_clamp ? _highBound.x : outHighBound.x));
            float valueY =  Mathf.Clamp(value.y, (_clamp && !waitToLoad ? _lowBound.y : outLowBound.y), (_clamp ? _highBound.y : outHighBound.y));
            var scrollOffset =  new Vector2(valueX, valueY);
            contentContainer.transform.position = scrollOffset;
            if ((value.x > _highBound.x || value.y < _lowBound.y) && !_waiting)
            {
                reachEnd?.Invoke();
                _waiting = true;
            }
        }
    }

    private Vector2 scrollVelocity
    {
        get => _scrollVelocity;
        set
        {
            _scrollVelocity = value;
            scrollOffset += _scrollVelocity;
        }
    }

    private Vector2 springBackVelocity
    {
        get => _springBackVelocity;
        set
        {
            _springBackVelocity = value;
            scrollOffset += value;
        }
    }
    #endregion
    
    
    public override VisualElement contentContainer { get; }
    
    
    public new class UxmlFactory : UxmlFactory<GridView, UxmlTraits> { }
    
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private UxmlEnumAttributeDescription<GridConstraint> _constraint = new() { name = "constraint", defaultValue = GridConstraint.ColumnFixedCount};
        private UxmlIntAttributeDescription _constraintCount = new() { name = "constraint-count", defaultValue = 2 };
        private UxmlIntAttributeDescription _margin = new() { name = "margin", defaultValue = 2 };
        private UxmlFloatAttributeDescription _scrollDecelerationRate = new() { name = "scroll-deceleration-rate", defaultValue = 0.005f };
        private UxmlFloatAttributeDescription _elasticity = new() { name = "elasticity", defaultValue = 0.1f };
        
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var ate = ve as GridView;
            
            ate.constraint = _constraint.GetValueFromBag(bag, cc);
            ate.constraintCount = _constraintCount.GetValueFromBag(bag, cc);
            ate.margin = _margin.GetValueFromBag(bag, cc);
            ate.scrollDecelerationRate = _scrollDecelerationRate.GetValueFromBag(bag, cc);
            ate.elasticity = _elasticity.GetValueFromBag(bag, cc);
            ate.Update();
        }
    }
    
    

    public GridView()
    {
        var container = new VisualElement {name =  "container", 
            style =
            {
                height =  Length.Auto(), width = Length.Percent(100),
                justifyContent = Justify.FlexStart,
                flexShrink = 0, flexGrow = 0
                
            } };
      
        hierarchy.Add(container);
        contentContainer = container.contentContainer;

        RegisterCallback<PointerMoveEvent>(OnPointerMove); 
        RegisterCallback<PointerDownEvent>(OnPointerDown);
        RegisterCallback<PointerUpEvent>(ReleaseScrolling);
        RegisterCallback<PointerCancelEvent>(ReleaseScrolling);
        RegisterCallback<PointerLeaveEvent>(ReleaseScrolling);
        RegisterCallback<PointerOutEvent>(ReleaseScrolling);
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }
    


    

    private void Update()
    { 
        Clear();
        switch (constraint)
        {
            case GridConstraint.ColumnFixedCount :
                contentContainer.style.flexDirection = FlexDirection.Column;
                break;
            case  GridConstraint.RowFixedCount :
                contentContainer.style.flexDirection = FlexDirection.Row;
                break;
        }
        if (constraintCount == 0 || itemsSource is null) return;
        CreateGrid();
        AddItems();
        OnGeometryChanged(null);
    }



    private void CreateGrid()
    {
        for (int i = 0; i < (itemsSource.Count + constraintCount - 1) / constraintCount; i++)
        {
            var alignment = new VisualElement()
            {
                style =
                {
                    width = Length.Percent(100),
                    justifyContent = Justify.SpaceAround,
                    alignItems = Align.Center,
                    flexDirection = constraint == GridConstraint.ColumnFixedCount? FlexDirection.Row : FlexDirection.Column
                }
            };

            contentContainer.hierarchy.Add(alignment);
        }
    }

    private void AddItems()
    {
        for (var i = 0; i < itemsSource.Count; i++)
        {
            var ve = makeItem();
            var child = contentContainer?.Children().ToArray()[i/constraintCount];
            bindItem(ve, itemsSource[i]);
            ve.style.flexGrow = 0;
            ve.style.flexShrink = 0;
            child.hierarchy.Add(ve);
            var itemSource = itemsSource[i];
            ve.RegisterCallback<PointerDownEvent>(e => _canClick = true);
            ve.RegisterCallback<ClickEvent>(e =>
            {
                if (_canClick) onSelect?.Invoke(ve, itemSource);
            });
        }
    }

    private void ReleaseScrolling<T>(T evt) where T : EventBase, IPointerEvent 
    {
        if (_pointerId != evt.pointerId) return;
        _pointerId = PointerId.invalidPointerId;
        _startedMoving = false;
        _touchStoppedVelocity = false;
        if (evt.deltaPosition.magnitude != 0)
        {
            scrollVelocity = new Vector2(0, evt.deltaPosition.y);
        }
        ExecuteElasticSpringAnimation();
        contentContainer.ReleasePointer(evt.pointerId);
    }

    private void ExecuteElasticSpringAnimation()
    {
        ComputeInitialSpringBackVelocity();
        if (_postPointerUpAnimation == null)
            _postPointerUpAnimation = schedule.Execute(PostPointerUpAnimation).Every(30L);
        else
            _postPointerUpAnimation.Resume();
    }
    
    private void PostPointerUpAnimation()
    {
        
        ApplyScrollInertia();
        SpringBack();
        if (!(springBackVelocity == Vector2.zero) || !(scrollVelocity == Vector2.zero))
            return;
        _postPointerUpAnimation.Pause();
    }

    private void ComputeInitialSpringBackVelocity()
    {
        Vector2 min = waitToLoad ? outLowBound : _lowBound;
        Vector2 max = _highBound;

        float yVelocity = 0;
        
        if (scrollOffset.y < min.y ) 
            yVelocity = _lowBound.y - scrollOffset.y;
        if (scrollOffset.y > max.y)
            yVelocity = _highBound.y - scrollOffset.y;
        
              
        float xVelocity = 0;
        /*if (scrollOffset.x < min.x ) 
            xVelocity = Mathf.Clamp(scrollOffset.x + _springSpeed, Single.NegativeInfinity, min.x)  - scrollOffset.x;
        if (scrollOffset.x > max.x)
            xVelocity = Mathf.Clamp(scrollOffset.x - _springSpeed, max.x, Single.PositiveInfinity) - scrollOffset.x;*/
        
        springBackVelocity = new Vector2(xVelocity,  yVelocity);
    }

    private void ApplyScrollInertia()
    {
        if (Mathf.Abs(scrollVelocity.magnitude) < 1.0f || IsOutBound())
        {
            scrollVelocity = Vector2.zero;
        }
        scrollVelocity *= Mathf.Pow(scrollDecelerationRate, Time.unscaledDeltaTime);
    }

    private void SpringBack()
    {

        if (IsInBound())
            _springBackVelocity = Vector2.zero;

        if (scrollOffset.y < _lowBound.y && !waitToLoad) 
            _springBackVelocity.y =  Mathf.SmoothDamp(scrollOffset.y, _lowBound.y, ref _springBackVelocity.y, elasticity, float.PositiveInfinity, Time.unscaledDeltaTime) - scrollOffset.y;
        else if (springBackVelocity.y > _highBound.y)
            _springBackVelocity.y = Mathf.SmoothDamp(scrollOffset.y, _highBound.y, ref _springBackVelocity.y, elasticity, float.PositiveInfinity, Time.unscaledDeltaTime) - scrollOffset.y;
        else
            _springBackVelocity.y = 0;
        springBackVelocity = _springBackVelocity;

    }

    private bool IsOutBound(bool waitToLoad = true)
    {
        bool verticalOut = scrollOffset.y <= (waitToLoad ? outLowBound.y : _lowBound.y) || scrollOffset.y > _highBound.y;
        bool horizontalOut = scrollOffset.x < _lowBound.x || scrollOffset.x >=  (waitToLoad? outHighBound.x : _highBound.x);
        return verticalOut || horizontalOut;
    }
    private bool IsInBound(bool waitToLoad = true) => !IsOutBound(waitToLoad);

    #region Event
    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        _lowBound = new (0 , resolvedStyle.height - contentContainer.resolvedStyle.height);
        _highBound = new(resolvedStyle.width-contentContainer.resolvedStyle.width, 0);
        
        
       
        float val = Mathf.Clamp( scrollOffset.y, waitToLoad ? outLowBound.y : _lowBound.y, _highBound.y);
        Vector3 pos = contentContainer.transform.position;
        pos.y = val;
        
      

        if (contentContainer.resolvedStyle.height <= resolvedStyle.height)
        {
            pos.y = 0;
        }
        contentContainer.transform.position = pos;
        contentContainer.style.paddingTop = margin/3;
        contentContainer.style.paddingBottom = margin/3;
        contentContainer.style.paddingLeft = margin/3;
        contentContainer.style.paddingRight = margin/3;

        foreach (var child in contentContainer.Children())
        {
            if (constraint == GridConstraint.ColumnFixedCount)
            {
               
                child.style.height =alignmentLength;

                foreach (var cell in child.Children())
                {
                    cell.style.height = alignmentLength - margin;
                    cell.style.width = alignmentLength - margin;
                }

            }
        }

    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_pointerId != evt.pointerId) return;
        var position = contentContainer.transform.position + evt.deltaPosition;
        if (contentContainer.transform.position != position) _canClick = false;
        position.x = contentContainer.transform.position.x;
        scrollOffset = position;
        _pointerStartPosition = evt.position;
        evt.StopPropagation();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (!evt.isPrimary) return;
        if (_pointerId != PointerId.invalidPointerId)
            ReleaseScrolling(evt);
        _postPointerUpAnimation?.Pause();
        _pointerId = evt.pointerId;
        springBackVelocity = Vector2.zero;
        scrollVelocity = Vector2.zero;
        _startedMoving = true;
        _touchStoppedVelocity = true;
        
        evt.StopPropagation();
    }

    private Vector2 GetDeltaPosition(Vector2 deltaPos)
        => constraint == GridConstraint.ColumnFixedCount ? new Vector2(0, deltaPos.y) :  new Vector2(deltaPos.x, 0);
    
    #endregion
}

public enum GridConstraint
{
    ColumnFixedCount,
    RowFixedCount
}