class Numeric
  def secs
    self
  end
  alias :secs :sec

  def millisecs
    self * 0.001
  end
  alias :millisecs, :millisec
end
