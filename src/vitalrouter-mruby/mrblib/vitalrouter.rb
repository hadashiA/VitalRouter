module VitalRouter
  class SharedState
    def has?(key)
      @variables&.key?(key)
    end

    def [](key)
      @variables&.[](key)
    end

    private

    def []=(key, val)
      (@variables ||= {})[key] = val
    end
  end

  def state
    @shared_state ||= SharedState.new
  end

  def cmd(name, **props)
    camelized_props = props.reduce({}) { |h, (k, v)|
      h[camelize(k.to_s)] = v
      h
    }
    __cmd Fiber.current, name.to_s, camelized_props
  end

  def log(message)
    __cmd Fiber.current, 'vitalrouter:log', message.to_s
  end

  def wait(sec)
    __cmd Fiber.current, 'vitalrouter:wait', MessagePack.pack(sec)
  end

  private

  def camelize(input)
    input.split('_').map {|word| word[0].upcase + word[1..] }.join
  end
end


Object.include VitalRouter
