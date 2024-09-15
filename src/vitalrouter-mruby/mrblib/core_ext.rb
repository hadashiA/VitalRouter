class Numeric
  def secs
    VitalRouter::TimeDuration.new self
  end
  alias :secs :sec

  def millisecs
    VitalRouter::TimeDuration.new self * 0.001
  end
  alias :millisecs, :millisec

  def frames
    VitalRouter::FrameDuration.new self.to_i
  end
  alias :frames, :frame
end
